using ILP.Shared.TransactionDocument.Model.Dto;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ILP.Shared.Test.TransactionDocument.Model.Dto
{
    public class DocumentItemDtoTest
    {
        [Theory]
        // 1. Does not require rounding
        [InlineData(5, 20.00, 100.00)]
        [InlineData(2, 12.50, 25.00)]
        [InlineData(3, 10.25, 30.75)]
        [InlineData(2.5, 4.10, 10.25)]
        [InlineData(0, 19.99, 0.00)]

        // 2. Midpoint (.xx5) rounding to even
        [InlineData(1.5, 1.17, 1.76)] // 1.755 -> rounds up to even (.76)
        [InlineData(1.5, 1.15, 1.72)] // 1.725 -> rounds down to even (.72)
        [InlineData(2.5, 0.55, 1.38)] // 1.375 -> rounds up to even (.38)
        [InlineData(2.5, 0.65, 1.62)] // 1.625 -> rounds down to even (.62)

        // 3. Non-midpoint rounding (< .xx5 and > .xx5)
        [InlineData(1.2, 2.11, 2.53)] // 2.532 -> rounds down
        [InlineData(1.2, 2.14, 2.57)] // 2.568 -> rounds up
        public void GetBasePrice_AppliesExpectedRounding(decimal quantity, decimal unitPrice, decimal expectedBasePrice)
        {
            var dto = new DocumentItemDto(quantity, unitPrice);
            Assert.Equal(expectedBasePrice, dto.GetBasePrice());
        }


        [Theory]
        // Midpoint (.xx5) with 9% GST -> Round down to even
        [InlineData(1, 0.50, 0.09, 0.04)]  // 0.50 * 0.09 = 0.045 -> 0.04
        [InlineData(1, 2.50, 0.09, 0.22)]  // 2.50 * 0.09 = 0.225 -> 0.22
        [InlineData(1, 20.50, 0.09, 1.84)] // 20.50 * 0.09 = 1.845 -> 1.84

        // Midpoint (.xx5) with 9% GST -> Round up to even
        [InlineData(1, 1.50, 0.09, 0.14)]  // 1.50 * 0.09 = 0.135 -> 0.14
        [InlineData(1, 3.50, 0.09, 0.32)]  // 3.50 * 0.09 = 0.315 -> 0.32
        [InlineData(1, 15.50, 0.09, 1.40)] // 15.50 * 0.09 = 1.395 -> 1.40
        [InlineData(1, 55.50, 0.09, 5.00)] // 55.50 * 0.09 = 4.995 -> 5.00

        // Zero-tax edge case
        [InlineData(10, 15.50, 0.00, 0.00)]
        public void GetTaxAmount_AppliesExpectedRounding(decimal quantity, decimal unitPrice, decimal taxRate, decimal expectedTaxAmount)
        {
            var dto = new DocumentItemDto(quantity, unitPrice, taxRate: taxRate);
            Assert.Equal(expectedTaxAmount, dto.GetTaxAmount());
        }

        [Theory]
        // Base rounded UP to even + Tax rounded UP
        [InlineData(1.5, 1.17, 0.09, 1.92)]  // Base: 1.76, Tax: 0.16 -> Total: 1.92

        // Base rounded DOWN to even + Tax rounded DOWN
        [InlineData(1.5, 1.15, 0.09, 1.87)]  // Base: 1.72, Tax: 0.15 -> Total: 1.87

        // Exact midpoint tax rounded DOWN to even
        [InlineData(1.0, 0.50, 0.09, 0.54)]  // Base: 0.50, Tax: 0.04 -> Total: 0.54

        // Exact midpoint tax rounded UP to even
        [InlineData(1.0, 1.50, 0.09, 1.64)]  // Base: 1.50, Tax: 0.14 -> Total: 1.64

        // Dollar rollover cases
        [InlineData(1.0, 55.50, 0.09, 60.50)] // Base: 55.50, Tax: 5.00 -> Total: 60.50
        [InlineData(1.0, 0.95, 0.09, 1.04)]   // Base: 0.95, Tax: 0.09 -> Total: 1.04

        // Edge cases: Zero tax and zero quantity
        [InlineData(4.0, 12.50, 0.00, 50.00)] // Zero tax rate
        [InlineData(0.0, 99.99, 0.09, 0.00)]  // Zero quantity
        public void GetTotalAmount_CalculatesSumOfRoundedBaseAndTax(
            decimal quantity,
            decimal unitPrice,
            decimal taxRate,
            decimal expectedTotalAmount)
        {
            // Test execution against DocumentItemDto
            var dto = new DocumentItemDto(quantity, unitPrice, taxRate: taxRate);
            Assert.Equal(expectedTotalAmount, dto.GetTotalAmount());
        }
    }
}

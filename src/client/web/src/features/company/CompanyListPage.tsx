import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TableSortLabel,
  TextField,
  Typography,
} from '@mui/material'
import { IconAction } from '../../common/components/IconAction'
import { CompanyDetailsDialog } from './components/CompanyDetailsDialog'
import { searchCompanies } from './service/company-service'
import type { CompanySummary } from './model/company-model'

type CompanySortField = 'name' | 'totalOutstandingSgd'
const emptyCompanies: CompanySummary[] = []

export function CompanyListPage() {
  const [search, setSearch] = useState('')
  const [result, setResult] = useState<{
    search: string
    companies: CompanySummary[]
    error: string | null
  } | null>(null)
  const [selectedCompanyId, setSelectedCompanyId] = useState<number | null>(null)
  const [sortField, setSortField] = useState<CompanySortField>('name')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')

  useEffect(() => {
    let active = true
    searchCompanies(search)
      .then((result) => {
        if (active) setResult({ search, companies: result.items, error: null })
      })
      .catch((reason: unknown) => {
        if (active) {
          setResult({
            search,
            companies: [],
            error: reason instanceof Error ? reason.message : 'Companies could not be loaded.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [search])

  const loading = result?.search !== search
  const error = loading ? null : result?.error ?? null
  const companies = loading || error ? emptyCompanies : result?.companies ?? emptyCompanies

  const sortedCompanies = useMemo(
    () =>
      [...companies].sort((left, right) => {
        const compared =
          sortField === 'name'
            ? left.name.localeCompare(right.name)
            : left.totalOutstandingSgd - right.totalOutstandingSgd
        return sortDirection === 'asc' ? compared : -compared
      }),
    [companies, sortField, sortDirection],
  )

  const sortBy = (field: CompanySortField) => {
    if (field === sortField) {
      setSortDirection((direction) => (direction === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortField(field)
      setSortDirection('asc')
    }
  }

  return (
    <>
      <section className="page-heading">
        <div>
          <Typography variant="h4" component="h1">Companies</Typography>
          <Typography color="text.secondary">View company information and recent documents.</Typography>
        </div>
        <TextField
          label="Search by UEN or company name"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          size="small"
        />
      </section>
      {error && <Alert severity="error">{error}</Alert>}
      {loading && <CircularProgress aria-label="Loading companies" />}
      {!loading && !error && (
        <Paper variant="outlined" className="company-table">
          <Table aria-label="Companies">
            <TableHead>
              <TableRow>
                <TableCell>UEN</TableCell>
                <TableCell>
                  <TableSortLabel
                    active={sortField === 'name'}
                    direction={sortField === 'name' ? sortDirection : 'asc'}
                    onClick={() => sortBy('name')}
                  >
                    Name
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right">
                  <TableSortLabel
                    active={sortField === 'totalOutstandingSgd'}
                    direction={sortField === 'totalOutstandingSgd' ? sortDirection : 'asc'}
                    onClick={() => sortBy('totalOutstandingSgd')}
                  >
                    Total Outstanding (SGD)
                  </TableSortLabel>
                </TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {sortedCompanies.map((company) => (
                <TableRow key={company.id} hover>
                  <TableCell>{company.uen}</TableCell>
                  <TableCell>{company.name}</TableCell>
                  <TableCell align="right">
                    {new Intl.NumberFormat('en-SG', { style: 'currency', currency: 'SGD' }).format(company.totalOutstandingSgd)}
                  </TableCell>
                  <TableCell align="right">
                    <IconAction
                      kind="view"
                      label={`View ${company.name}`}
                      onClick={() => setSelectedCompanyId(company.id)}
                    />
                  </TableCell>
                </TableRow>
              ))}
              {sortedCompanies.length === 0 && (
                <TableRow><TableCell colSpan={4} align="center">No companies found.</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </Paper>
      )}
      <CompanyDetailsDialog
        companyId={selectedCompanyId}
        onClose={() => setSelectedCompanyId(null)}
      />
    </>
  )
}

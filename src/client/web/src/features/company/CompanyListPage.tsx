import { useEffect, useState } from 'react'
import {
  Alert,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TablePagination,
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
const defaultPageSize = 20

export function CompanyListPage() {
  const [search, setSearch] = useState('')
  const [result, setResult] = useState<{
    search: string
    companies: CompanySummary[]
    totalCount: number
    page: number
    pageSize: number
    sortField: CompanySortField
    sortDirection: 'asc' | 'desc'
    error: string | null
  } | null>(null)
  const [selectedCompanyId, setSelectedCompanyId] = useState<number | null>(null)
  const [sortField, setSortField] = useState<CompanySortField>('name')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(defaultPageSize)

  useEffect(() => {
    let active = true
    searchCompanies(search, {
      page: page + 1,
      pageSize,
      sortBy: sortField,
      sortDirection,
    })
      .then((result) => {
        if (active) {
          setResult({
            search,
            companies: result.items,
            totalCount: result.totalCount,
            page,
            pageSize,
            sortField,
            sortDirection,
            error: null,
          })
        }
      })
      .catch((reason: unknown) => {
        if (active) {
          setResult({
            search,
            companies: [],
            totalCount: 0,
            page,
            pageSize,
            sortField,
            sortDirection,
            error: reason instanceof Error ? reason.message : 'Companies could not be loaded.',
          })
        }
      })
    return () => {
      active = false
    }
  }, [search, page, pageSize, sortField, sortDirection])

  const loading =
    result?.search !== search
    || result?.page !== page
    || result?.pageSize !== pageSize
    || result?.sortField !== sortField
    || result?.sortDirection !== sortDirection
  const error = loading ? null : result?.error ?? null
  const companies = loading || error ? emptyCompanies : result?.companies ?? emptyCompanies

  const sortBy = (field: CompanySortField) => {
    setPage(0)
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
          onChange={(event) => {
            setSearch(event.target.value)
            setPage(0)
          }}
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
              {companies.map((company) => (
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
              {companies.length === 0 && (
                <TableRow><TableCell colSpan={4} align="center">No companies found.</TableCell></TableRow>
              )}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={result?.totalCount ?? 0}
            page={page}
            rowsPerPage={pageSize}
            rowsPerPageOptions={[10, 20, 50]}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value))
              setPage(0)
            }}
          />
        </Paper>
      )}
      <CompanyDetailsDialog
        companyId={selectedCompanyId}
        onClose={() => setSelectedCompanyId(null)}
      />
    </>
  )
}

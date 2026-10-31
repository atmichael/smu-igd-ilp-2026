import { useMemo, useState } from 'react'
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TableSortLabel,
  TextField,
} from '@mui/material'
import { IconAction } from '../../../common/components/IconAction'
import type { DocumentRecord } from '../model/document-model'

type SortField = 'refNumber' | 'companyUen' | 'dueDate'
type ConfidenceFilter = 'all' | 'low' | 'medium' | 'high'

interface DocumentDataGridProps {
  rows: DocumentRecord[]
  onView: (document: DocumentRecord) => void
  onEdit: (document: DocumentRecord) => void
  onDelete: (document: DocumentRecord) => void | Promise<void>
}

function confidenceMatches(value: number | null | undefined, filter: ConfidenceFilter): boolean {
  if (filter === 'all') return true
  if (value == null) return false
  if (filter === 'low') return value < 0.6
  if (filter === 'medium') return value >= 0.6 && value < 0.8
  return value >= 0.8
}

function displayDate(value: string | null | undefined): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('en-SG')
}

function displayAmount(value: number): string {
  return new Intl.NumberFormat('en-SG', { style: 'currency', currency: 'SGD' }).format(value)
}

export function DocumentDataGrid({ rows, onView, onEdit, onDelete }: DocumentDataGridProps) {
  const [uenFilter, setUenFilter] = useState('')
  const [dueDateFilter, setDueDateFilter] = useState('')
  const [confidenceFilter, setConfidenceFilter] = useState<ConfidenceFilter>('all')
  const [sortField, setSortField] = useState<SortField>('refNumber')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')
  const [page, setPage] = useState(0)
  const [rowsPerPage, setRowsPerPage] = useState(25)
  const [pendingDelete, setPendingDelete] = useState<DocumentRecord | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const closeDeleteDialog = () => {
    if (deleting) return
    setPendingDelete(null)
    setDeleteError(null)
  }

  const confirmDelete = async () => {
    if (!pendingDelete) return
    setDeleting(true)
    setDeleteError(null)
    try {
      await onDelete(pendingDelete)
      setPendingDelete(null)
    } catch (reason: unknown) {
      setDeleteError(reason instanceof Error ? reason.message : 'The document could not be deleted.')
    } finally {
      setDeleting(false)
    }
  }

  const visibleRows = useMemo(() => {
    const filtered = rows.filter((row) => {
      const uen = row.company?.uen ?? ''
      return (
        uen.toLowerCase().includes(uenFilter.trim().toLowerCase()) &&
        (!dueDateFilter || row.dueDate?.slice(0, 10) === dueDateFilter) &&
        confidenceMatches(row.confidence, confidenceFilter)
      )
    })
    return filtered.sort((left, right) => {
      const leftValue =
        sortField === 'companyUen'
          ? left.company?.uen ?? ''
          : sortField === 'dueDate'
            ? left.dueDate ?? ''
            : left.refNumber
      const rightValue =
        sortField === 'companyUen'
          ? right.company?.uen ?? ''
          : sortField === 'dueDate'
            ? right.dueDate ?? ''
            : right.refNumber
      const compared = leftValue.localeCompare(rightValue, undefined, { numeric: true })
      return sortDirection === 'asc' ? compared : -compared
    })
  }, [rows, uenFilter, dueDateFilter, confidenceFilter, sortField, sortDirection])

  const selectSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection((direction) => (direction === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortField(field)
      setSortDirection('asc')
    }
    setPage(0)
  }

  const sortLabel = (field: SortField, label: string) => (
    <TableSortLabel
      active={sortField === field}
      direction={sortField === field ? sortDirection : 'asc'}
      onClick={() => selectSort(field)}
    >
      {label}
    </TableSortLabel>
  )

  return (
    <Paper variant="outlined" className="document-grid">
      <div className="document-grid__filters">
        <TextField
          label="Filter by company UEN"
          size="small"
          value={uenFilter}
          onChange={(event) => {
            setUenFilter(event.target.value)
            setPage(0)
          }}
        />
        <TextField
          label="Filter by due date"
          type="date"
          size="small"
          value={dueDateFilter}
          onChange={(event) => {
            setDueDateFilter(event.target.value)
            setPage(0)
          }}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          label="Filter by confidence level"
          select
          size="small"
          value={confidenceFilter}
          onChange={(event) => {
            setConfidenceFilter(event.target.value as ConfidenceFilter)
            setPage(0)
          }}
        >
          <MenuItem value="all">All levels</MenuItem>
          <MenuItem value="low">Below 60%</MenuItem>
          <MenuItem value="medium">60–79%</MenuItem>
          <MenuItem value="high">80% and above</MenuItem>
        </TextField>
      </div>
      <TableContainer>
        <Table aria-label="Processed documents">
          <TableHead>
            <TableRow>
              <TableCell>{sortLabel('refNumber', 'Document Number')}</TableCell>
              <TableCell>{sortLabel('companyUen', 'Company UEN & Name')}</TableCell>
              <TableCell>Posting Date</TableCell>
              <TableCell>{sortLabel('dueDate', 'Due Date')}</TableCell>
              <TableCell align="right">Total Amount</TableCell>
              <TableCell>Confidence Level</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {visibleRows.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage).map((row) => (
              <TableRow key={row.id} hover>
                <TableCell data-label="Document Number">{row.refNumber}</TableCell>
                <TableCell data-label="Company UEN & Name">
                  <strong>{row.company?.uen ?? '—'}</strong>
                  <br />
                  {row.company?.name ?? '—'}
                </TableCell>
                <TableCell data-label="Posting Date">{displayDate(row.sentDate)}</TableCell>
                <TableCell data-label="Due Date">{displayDate(row.dueDate)}</TableCell>
                <TableCell align="right" data-label="Total Amount">{displayAmount(row.totalAmount)}</TableCell>
                <TableCell data-label="Confidence Level">
                  {row.confidence == null ? '—' : `${Math.round(row.confidence * 100)}%`}
                </TableCell>
                <TableCell align="right" className="document-grid__actions" data-label="Actions">
                  <IconAction kind="view" label={`View ${row.refNumber}`} onClick={() => onView(row)} />
                  <IconAction kind="edit" label={`Edit ${row.refNumber}`} onClick={() => onEdit(row)} />
                  <IconAction
                    kind="delete"
                    label={`Delete ${row.refNumber}`}
                    onClick={() => setPendingDelete(row)}
                  />
                </TableCell>
              </TableRow>
            ))}
            {visibleRows.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} align="center">
                  No documents found.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        component="div"
        count={visibleRows.length}
        page={page}
        onPageChange={(_event, nextPage) => setPage(nextPage)}
        rowsPerPage={rowsPerPage}
        onRowsPerPageChange={(event) => {
          setRowsPerPage(Number(event.target.value))
          setPage(0)
        }}
        rowsPerPageOptions={[25, 50, 100]}
        labelRowsPerPage="Rows per page:"
      />
      <Dialog open={pendingDelete !== null} onClose={closeDeleteDialog}>
        <DialogTitle>Delete document</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete document {pendingDelete?.refNumber}?
          </DialogContentText>
          {deleteError && <Alert severity="error">{deleteError}</Alert>}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDeleteDialog} disabled={deleting}>Cancel</Button>
          <Button onClick={confirmDelete} color="error" disabled={deleting}>Confirm</Button>
        </DialogActions>
      </Dialog>
    </Paper>
  )
}

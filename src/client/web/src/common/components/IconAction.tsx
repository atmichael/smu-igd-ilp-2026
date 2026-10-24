import { IconButton } from '@mui/material'

type IconActionKind = 'view' | 'edit' | 'delete'

interface IconActionProps {
  kind: IconActionKind
  label: string
  onClick: () => void
}

const paths: Record<IconActionKind, string> = {
  view: 'M12 5c-5 0-9.27 3.11-11 7 1.73 3.89 6 7 11 7s9.27-3.11 11-7c-1.73-3.89-6-7-11-7zm0 11.5a4.5 4.5 0 1 1 0-9 4.5 4.5 0 0 1 0 9zm0-2a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5z',
  edit: 'M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zm17.71-10.04a1 1 0 0 0 0-1.41l-2.51-2.51a1 1 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 2-1.66z',
  delete: 'M6 19c0 1.1.9 2 2 2h8a2 2 0 0 0 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z',
}

export function IconAction({ kind, label, onClick }: IconActionProps) {
  return (
    <IconButton aria-label={label} title={label} onClick={onClick} sx={{ minWidth: 44, minHeight: 44 }}>
      <svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24">
        <path d={paths[kind]} fill="currentColor" />
      </svg>
    </IconButton>
  )
}

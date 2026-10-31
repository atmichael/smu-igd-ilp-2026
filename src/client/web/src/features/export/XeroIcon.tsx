export function XeroIcon({ size = 24 }: { size?: number }) {
  return (
    <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24">
      <circle cx="12" cy="12" r="12" fill="#13B5EA" />
      <path
        d="M7.2 8.2l3.1 3.8-3.1 3.8m9.6-7.6L13.7 12l3.1 3.8"
        fill="none"
        stroke="#ffffff"
        strokeWidth="1.8"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

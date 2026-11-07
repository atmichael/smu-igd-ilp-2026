import { apiJson } from '../../../common/service/api-client'

export function getUploadConfig(): Promise<{ attachmentLimitInMB: number }> {
  return apiJson('/api/config')
}

import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface UploadFileDto {
  fileName: string;
  contentType: string;
  contentBase64: string;
  description?: string;
}

export interface UploadedFileDto extends FullAuditedEntityDto<string> {
  fileName?: string;
  contentType?: string;
  fileSize: number;
  description?: string;
  contentBase64?: string;
}

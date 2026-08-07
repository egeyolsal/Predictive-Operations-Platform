export interface UserAdminListDto {
  id: number;
  username: string;
  email: string;
  phoneNumber?: string;
  role: string;
}

export interface UpdateUserRoleDto {
  role: string;
}

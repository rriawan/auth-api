import { TablePageSkeleton } from "@/components/shared/page-skeletons";

export default function UsersLoading() {
  return <TablePageSkeleton title="Memuat Daftar Pengguna..." rowCount={6} />;
}


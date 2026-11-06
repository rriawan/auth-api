import { TablePageSkeleton } from "@/components/shared/page-skeletons";

export default function EmployeesLoading() {
  return <TablePageSkeleton title="Memuat Daftar Pegawai..." rowCount={6} />;
}

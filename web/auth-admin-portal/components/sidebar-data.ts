import {
  Building2,
  History,
  House,
  Layers,
  User,
  UserRoundCog,
  UserRoundX,
  UsersRound,
} from "lucide-react";

export const data = {
  navMain: [
    {
      title: "Dashboard",
      url: "/",
      icon: House,
      isActive: true,
    },
    {
      label: "Manajemen Akses",
    },
    {
      title: "Aplikasi",
      url: "/apps",
      icon: Layers,
    },
    {
      title: "Pengguna",
      url: "/users",
      icon: UsersRound,
    },
    {
      label: "Organisasi & Pegawai",
    },
    {
      title: "Pegawai",
      url: "/employees",
      icon: UserRoundCog,
    },
    {
      title: "Lifecycle Akun",
      url: "/employees/lifecycle",
      icon: UserRoundX,
    },
    {
      title: "Kantor",
      url: "/organization/offices",
      icon: Building2,
    },
    {
      label: "Keamanan",
    },
    {
      title: "Audit Logs",
      url: "/audit-logs",
      icon: History,
    },
    {
      label: "Akun Saya",
    },
    {
      title: "Profil & Password",
      url: "/profile",
      icon: User,
    },
  ],
};

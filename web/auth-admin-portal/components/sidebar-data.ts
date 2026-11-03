import {
  History,
  House,
  Layers,
  User,
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

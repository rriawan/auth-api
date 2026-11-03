"use client";

import { Loader2, LogOutIcon } from "lucide-react";
import { startTransition, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "../ui/button";
import { doLogout } from "./actions/logout";

const Logout = () => {
  const [loading, setLoading] = useState(false);

  const handleLogout = () => {
    setLoading(true);

    startTransition(async () => {
      try {
        toast.loading("Sedang keluar...", { id: "logout-toast" });
        await doLogout();
        toast.success("Berhasil keluar.", { id: "logout-toast" });
      } catch (error) {
        console.error("Logout error:", error);
        toast.error("Gagal keluar. Silakan coba kembali.", {
          id: "logout-toast",
        });
      } finally {
        setLoading(false);
      }
    });
  };

  return (
    <Button
      onClick={handleLogout}
      type="button"
      className={`hover:text-red-600 flex items-center gap-3 py-0.5 text-neutral-900 dark:text-white text-base !px-0.5 cursor-pointer leading-[0] w-full justify-start hover:no-underline h-[25px] ${loading ? "text-red-600" : ""
        }`}
      variant="link"
      disabled={loading}
    >
      {loading ? (
        <>
          <Loader2 className="animate-spin !w-4.5 !h-4.5" />
          Sedang keluar...
        </>
      ) : (
        <>
          <LogOutIcon width={24} height={24} className="!w-4.5 !h-4.5" />
          Keluar
        </>
      )}
    </Button>
  );
};

export default Logout;

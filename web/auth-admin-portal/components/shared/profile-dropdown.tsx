import Logout from "@/components/auth/logout";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";
import userImg from "@/public/assets/images/user.png";
import { User } from "lucide-react";
import { useSession } from "next-auth/react";
import Image from "next/image";
import Link from "next/link";

const ProfileDropdown = () => {
  const { data: session } = useSession();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          size="icon"
          className={cn(
            "rounded-full sm:w-10 sm:h-10 w-8 h-8 bg-gray-200/75 hover:bg-slate-200 focus-visible:ring-0 dark:bg-slate-700 dark:hover:bg-slate-600 border-0 cursor-pointer data-[state=open]:bg-gray-300 data-[state=open]:ring-4 data-[state=open]:ring-slate-300 dark:data-[state=open]:ring-slate-500 dark:data-[state=open]:bg-slate-600"
          )}
        >
          {session?.user?.image ? (
            <Image
              src={session.user.image}
              className="rounded-full"
              width={40}
              height={40}
              alt={session?.user?.name ?? "User profile"}
            />
          ) : (
            <Image
              src={userImg}
              className="rounded-full"
              width={40}
              height={40}
              alt="User profile"
            />
          )}
        </Button>
      </DropdownMenuTrigger>

      <DropdownMenuContent
        className="sm:w-[280px] min-w-[240px] right-[40px] absolute p-4 rounded-2xl overflow-hidden shadow-lg"
        side="bottom"
        align="end"
      >
        <div className="py-3 px-4 rounded-lg bg-primary/10 dark:bg-primary/20 flex items-center justify-between">
          <div>
            <h6 className="text-base text-neutral-900 dark:text-white font-semibold mb-0">
              {session?.user?.fullName ||
                session?.user?.username ||
                session?.user?.name ||
                "Administrator"}
            </h6>
            <span className="text-xs text-neutral-500 dark:text-neutral-300">
              {session?.user?.email ||
                session?.user?.roles?.[0] ||
                "SuperAdmin"}
            </span>
          </div>
        </div>

        <div className="pt-4">
          <ul className="flex flex-col gap-3">
            <li>
              <Link
                href="/profile"
                className="text-neutral-700 dark:text-white hover:text-primary dark:hover:text-primary flex items-center gap-3 text-sm font-medium"
              >
                <User className="w-4.5 h-4.5" /> Profil & Password
              </Link>
            </li>
            <li className="pt-2 border-t border-neutral-100 dark:border-slate-700">
              <Logout />
            </li>
          </ul>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
};

export default ProfileDropdown;

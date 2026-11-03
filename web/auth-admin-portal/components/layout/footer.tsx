const Footer = () => {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="d-footer bg-white dark:bg-[#273142] py-4 px-[22px] mt-[auto] border-t border-neutral-100 dark:border-slate-800">
      <div className="flex items-center justify-between gap-3 flex-wrap text-xs text-neutral-500 dark:text-neutral-400">
        <p className="mb-0">
          &copy; {currentYear} Auth Admin Portal. All Rights Reserved.
        </p>
        <p className="mb-0">
          Layanan Terpusat Autentikasi &amp; Otorisasi
        </p>
      </div>
    </footer>
  );
};

export default Footer;

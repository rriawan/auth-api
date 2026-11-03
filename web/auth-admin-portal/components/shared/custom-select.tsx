"use client";

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface CustomSelectProps {
  placeholder?: string;
  options: string[];
  value?: string;
  onChange?: (value: string) => void;
}

function CustomSelect({ placeholder = "Pilih", options, value, onChange }: CustomSelectProps) {
  return (
    <Select value={value} onValueChange={onChange}>
      <SelectTrigger className="min-w-[88px] focus-visible:shadow-none focus-visible:ring-0 font-medium dark:bg-slate-700 text-neutral-900 dark:text-white border border-slate-300 dark:border-slate-500 data-[placeholder]:text-neutral-900">
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <SelectItem key={option} value={option}>
            {option}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

export default CustomSelect;

"use client";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import type { ApexOptions } from "apexcharts";
import { BarChart3 } from "lucide-react";
import dynamic from "next/dynamic";

const ReactApexChart = dynamic(() => import("react-apexcharts"), {
  ssr: false,
});

export interface DailyLoginPoint {
  dateLabel: string;
  successCount: number;
  failedCount: number;
}

interface LoginActivityChartProps {
  data: DailyLoginPoint[];
}

export function LoginActivityChart({ data }: LoginActivityChartProps) {
  const categories = data.map((d) => d.dateLabel);
  const successSeries = data.map((d) => d.successCount);
  const failedSeries = data.map((d) => d.failedCount);

  const series = [
    {
      name: "Login Berhasil",
      data: successSeries,
    },
    {
      name: "Login Gagal / Lockout",
      data: failedSeries,
    },
  ];

  const options: ApexOptions = {
    chart: {
      type: "bar",
      height: 290,
      toolbar: {
        show: false,
      },
      fontFamily: "inherit",
    },
    colors: ["#10b981", "#ef4444"],
    plotOptions: {
      bar: {
        horizontal: false,
        columnWidth: "48%",
        borderRadius: 4,
      },
    },
    dataLabels: {
      enabled: false,
    },
    stroke: {
      show: true,
      width: 2,
      colors: ["transparent"],
    },
    xaxis: {
      categories,
      axisBorder: {
        show: false,
      },
      axisTicks: {
        show: false,
      },
      labels: {
        style: {
          colors: "#64748b",
          fontSize: "11px",
        },
      },
    },
    yaxis: {
      min: 0,
      forceNiceScale: true,
      labels: {
        style: {
          colors: "#64748b",
          fontSize: "11px",
        },
        formatter: (val: number) => Math.round(val).toString(),
      },
    },
    grid: {
      borderColor: "rgba(148, 163, 184, 0.15)",
      strokeDashArray: 4,
    },
    legend: {
      position: "top",
      horizontalAlign: "right",
      fontSize: "12px",
      markers: {
        size: 6,
      },
    },
    tooltip: {
      y: {
        formatter: (val: number) => `${val} aktivitas`,
      },
    },
  };

  return (
    <Card className="border border-neutral-200 dark:border-slate-800 shadow-sm">
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
              <BarChart3 className="w-4 h-4" />
            </div>
            <div>
              <CardTitle className="text-base font-semibold">
                Tren Aktivitas Login (7 Hari Terakhir)
              </CardTitle>
              <CardDescription className="text-xs">
                Perbandingan jumlah autentikasi berhasil dan gagal/lockout
                harian lintas aplikasi.
              </CardDescription>
            </div>
          </div>
        </div>
      </CardHeader>

      <CardContent className="pt-2">
        <div className="min-h-[290px] w-full">
          <ReactApexChart
            options={options}
            series={series}
            type="bar"
            height={290}
          />
        </div>
      </CardContent>
    </Card>
  );
}


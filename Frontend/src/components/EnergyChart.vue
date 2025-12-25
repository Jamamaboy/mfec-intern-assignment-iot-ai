<script setup>
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js'
import { computed } from 'vue'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Legend, Filler)

const props = defineProps(['realData', 'predictData', 'timeRange'])

// Helper function: แปลงเวลาให้สวยและไม่เป็น NaN
const formatTime = (timeStr) => {
  if (!timeStr) return ''
  const date = new Date(timeStr)
  if (isNaN(date)) return timeStr

  // ถ้าดู 24H ให้โชว์เวลา (10:00)
  // ถ้าดู 7D/30D ให้โชว์วันที่ (25 Dec)
  if (props.timeRange === '24H') {
      return date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
  } else {
      return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })
  }
}

const chartData = computed(() => {
  // 1. ประกาศตัวแปร dataPoints ก่อน (แก้จุดผิด)
  const dataPoints = props.realData || []

  // 2. ฟังก์ชันสร้าง Gradient
  const getGradient = (context) => {
    const ctx = context.chart.ctx;
    const gradient = ctx.createLinearGradient(0, 0, 0, 300);
    gradient.addColorStop(0, 'rgba(59, 130, 246, 0.5)'); // สีฟ้า MFEC
    gradient.addColorStop(1, 'rgba(59, 130, 246, 0.0)');
    return gradient;
  };

  // 3. Return Object ที่ถูกต้อง
  return {
    labels: dataPoints.map(d => formatTime(d.time)), // เรียกใช้ formatTime ที่เราเขียนไว้
    datasets: [
      {
        label: 'Historical Usage',
        backgroundColor: (context) => getGradient(context),
        borderColor: '#3b82f6',
        borderWidth: 2,
        data: dataPoints.map(d => d.value),
        fill: true,
        tension: 0.4,
        pointRadius: 0,
        pointHoverRadius: 6
      },
      {
        label: 'AI Forecast',
        borderColor: '#10b981',
        borderWidth: 2,
        borderDash: [5, 5],
        data: props.predictData?.map(d => d.value) || [],
        tension: 0.4,
        pointRadius: 0
      }
    ]
  }
})

const options = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    mode: 'index',
    intersect: false,
  },
  scales: {
    x: {
      grid: { display: false },
      ticks: {
        color: '#9ca3af',
        maxTicksLimit: 8 // จำกัดป้ายแกน X
      }
    },
    y: {
      grid: { color: 'rgba(255,255,255,0.1)' },
      ticks: { color: '#9ca3af' },
      beginAtZero: false
    }
  },
  plugins: {
    legend: { labels: { color: 'white', font: { family: 'Kanit' } } },
    tooltip: {
        backgroundColor: 'rgba(15, 23, 42, 0.9)',
        titleColor: '#fff',
        bodyColor: '#fff',
        borderColor: 'rgba(255,255,255,0.1)',
        borderWidth: 1
    }
  }
}
</script>

<template>
  <div class="h-64 w-full relative">
    <Line v-if="realData && realData.length > 0" :data="chartData" :options="options" />

    <div v-else class="absolute inset-0 flex items-center justify-center text-slate-500 text-sm">
        <span class="animate-pulse">Waiting for Data...</span>
    </div>
  </div>
</template>

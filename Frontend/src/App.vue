<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import axios from 'axios'
import { TresCanvas } from '@tresjs/core'
import { OrbitControls, Stars, Environment } from '@tresjs/cientos'
import Dashboard from './components/Dashboard.vue'
import Building3D from './components/Building3D.vue'

// --- Config ---
const API_URL = 'http://localhost:5069/api/energy'

// --- State ---
const showDashboard = ref(false)
const selectedBuilding = ref('')
const currentMW = ref(0)
const historyData = ref([])
const currentRange = ref('24h')

// --- Time State (Simulation) ---
const simulationTime = ref(null)
const formattedDate = ref('--')
const formattedTime = ref('--:--')

let pollingInterval = null

// --- Update UI Clock ---
const updateClock = (isoString) => {
  if (!isoString) return
  const date = new Date(isoString)
  simulationTime.value = date
  formattedDate.value = date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
  formattedTime.value = date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

// --- 1. Fetch Current (Real-time PUSH) ---
const fetchCurrent = async () => {
  try {
    const response = await axios.get(`${API_URL}/current`)
    if (response.data) {
        currentMW.value = response.data.value
        updateClock(response.data.timestamp)

        // Push data to graph if Dashboard is open AND not using custom range
        if (showDashboard.value && currentRange.value !== 'custom') {
           const newPoint = { time: response.data.timestamp, value: response.data.value }
           const last = historyData.value[historyData.value.length - 1]

           if (!last || last.time !== newPoint.time) {
               historyData.value.push(newPoint)
               if (historyData.value.length > 500) historyData.value.shift()
           }
        }
    }
  } catch (error) {
    console.error("❌ Error fetching current:", error)
  }
}

// --- 2. Fetch History (Load Data) ---
const fetchHistory = async (rangeOverride = null, customDates = null) => {
  if (!simulationTime.value) return

  try {
    const anchor = simulationTime.value.toISOString()
    const building = selectedBuilding.value
    let params = { building, anchor }

    // Case 1: Custom Date
    if (customDates) {
        currentRange.value = 'custom'
        params.range = 'custom'
        params.customStart = `${customDates.start}T00:00:00Z`
        params.customEnd = `${customDates.end}T23:59:59Z`
    }
    // Case 2: Range Button Clicked (24h, 7d, 6m, etc.)
    else if (rangeOverride) {
        currentRange.value = rangeOverride
        params.range = currentRange.value
    }
    // Case 3: Default Load
    else {
        params.range = currentRange.value
    }

    const response = await axios.get(`${API_URL}/history`, { params })

    historyData.value = response.data.map(item => ({
      time: item.time,
      value: item.value
    }))

  } catch (error) {
    console.error("❌ Error fetching history:", error)
  }
}

// --- Action: Open Building ---
const openBuilding = async (name) => {
  selectedBuilding.value = name
  showDashboard.value = true // ✅ เปิด Dashboard
  historyData.value = []
  currentRange.value = '24h'
  await fetchHistory()
}

// --- Lifecycle ---
onMounted(() => {
  fetchCurrent()
  pollingInterval = setInterval(fetchCurrent, 1000)
})

onUnmounted(() => {
  if (pollingInterval) clearInterval(pollingInterval)
})
</script>

<template>
  <div class="w-full h-screen bg-[#020617] text-slate-200 font-sans overflow-hidden relative selection:bg-blue-500/30">

    <nav class="absolute top-0 w-full z-10 p-6 flex justify-between items-start pointer-events-none select-none">
       <div class="flex items-center gap-4">
          <div class="text-right hidden md:block">
            <div class="text-[10px] text-slate-400 uppercase tracking-wider font-mono">SIMULATION TIME</div>
            <div class="text-sm font-bold text-slate-300 font-mono tracking-widest">{{ formattedDate }}</div>
            <div class="text-xl font-bold text-white font-mono tracking-widest leading-none">{{ formattedTime }}</div>
          </div>
          <div class="h-8 w-px bg-white/10 mx-2"></div>
       </div>

       <div class="flex flex-col text-right">
          <h1 class="text-3xl font-bold text-white tracking-tighter drop-shadow-2xl">
            MFEC <span class="text-blue-500">SMART GRID</span>
          </h1>
          <p class="text-slate-400 text-xs font-mono tracking-widest uppercase">Live System Active</p>
       </div>
    </nav>

    <TresCanvas clear-color="#020617" :pixel-ratio="1" power-preference="high-performance" shadows>
      <TresPerspectiveCamera :position="[20, 20, 20]" :look-at="[0, 0, 0]" :fov="45" />

      <OrbitControls make-default :enable-damping="true" :damping-factor="0.05"
        :auto-rotate="!showDashboard" :auto-rotate-speed="1.0"
        :enable-zoom="true" :min-distance="5" :max-distance="100" />

      <Stars :radius="100" :count="1500" :size="0.5" :fade="true" />
      <TresAmbientLight :intensity="1.5" />
      <TresDirectionalLight :position="[10, 20, 10]" :intensity="2" cast-shadow :shadow-mapSize="[1024, 1024]" />

      <TresMesh :rotation-x="-Math.PI/2" :position="[0, -0.5, 0]" receive-shadow>
        <TresPlaneGeometry :args="[100, 100]" />
        <TresMeshStandardMaterial color="#020617" :roughness="0.8" :metalness="0.2" />
      </TresMesh>

      <Suspense>
        <TresGroup>
            <Environment preset="city" :blur="1" :background="false" />

            <TresGroup @click="openBuilding('MFEC Tower')">
               <Building3D :position="[-4, 0, 0]" color="#3b82f6" label="MFEC Tower" :hideLabel="showDashboard" />
            </TresGroup>

            <TresGroup @click="openBuilding('Data Center')">
               <Building3D :position="[4, 0, 0]" color="#0ea5e9" label="Data Center" :hideLabel="showDashboard" />
            </TresGroup>
        </TresGroup>
      </Suspense>
    </TresCanvas>
    <Dashboard
      v-if="showDashboard"
      :buildingName="selectedBuilding"
      :currentMW="currentMW"
      :historyData="historyData"
      @close="showDashboard = false"
      @range-change="(r) => fetchHistory(r, null)"
      @custom-range="(dates) => fetchHistory(null, dates)"
    />

  </div>
</template>

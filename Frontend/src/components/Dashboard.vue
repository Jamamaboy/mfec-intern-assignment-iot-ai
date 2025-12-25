<script setup>
import { ref } from 'vue'
import EnergyChart from './EnergyChart.vue'

const props = defineProps(['buildingName', 'currentMW', 'historyData'])
const emit = defineEmits(['close', 'range-change', 'custom-range'])

const selectedRange = ref('24H')
const showCustomDate = ref(false)

// ✅ กลับมาใช้ตัวแปรวันที่แบบแยก เพื่อความเสถียรของ Browser
const startDate = ref('')
const endDate = ref('')

const setRange = (range) => {
  selectedRange.value = range
  showCustomDate.value = false
  emit('range-change', range.toLowerCase())
}

const applyCustomDate = () => {
  if (startDate.value && endDate.value) {
    selectedRange.value = 'Custom'
    emit('custom-range', {
        start: startDate.value,
        end: endDate.value
    })
  }
}


</script>

<template>
  <div class="absolute inset-0 z-50 flex justify-end">
    <div class="absolute inset-0 bg-slate-950/60 backdrop-blur-sm transition-opacity" @click="$emit('close')"></div>

    <div class="relative w-full max-w-lg h-full bg-[#0f172a] border-l border-white/10 shadow-2xl flex flex-col animate-slide-in">

      <div class="p-6 border-b border-white/10 flex justify-between items-center bg-white/5">
        <div>
          <div class="text-blue-400 text-xs font-bold uppercase tracking-wider mb-1">Building Monitor</div>
          <h2 class="text-2xl font-bold text-white">{{ buildingName }}</h2>
        </div>
        <button @click="$emit('close')" class="w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 text-white flex items-center justify-center transition">
          ✕
        </button>
      </div>

      <div class="p-6 grid grid-cols-2 gap-4">
        <div class="bg-gradient-to-br from-slate-800 to-slate-900 p-5 rounded-2xl border border-white/5 relative overflow-hidden group">
          <div class="absolute top-0 right-0 w-16 h-16 bg-blue-500/10 rounded-bl-full transition-all group-hover:bg-blue-500/20"></div>
          <p class="text-slate-400 text-sm">Current Load</p>
          <div class="text-3xl font-bold text-white mt-2">{{ currentMW.toLocaleString() }} <span class="text-sm font-normal text-slate-500">MW</span></div>
        </div>

        <div class="bg-gradient-to-br from-slate-800 to-slate-900 p-5 rounded-2xl border border-white/5 relative overflow-hidden">
          <p class="text-slate-400 text-sm">Carbon Emission</p>
          <div class="text-3xl font-bold text-emerald-400 mt-2">{{ (currentMW * 0.45).toFixed(0) }} <span class="text-sm font-normal text-slate-500">kg</span></div>
        </div>
      </div>

      <div class="flex-1 px-6 pb-6 min-h-0 flex flex-col">
        <div class="flex justify-between items-center mb-4 flex-wrap gap-2">
          <h3 class="text-white font-semibold">Consumption Trend</h3>

          <div class="flex gap-2">
            <div class="flex bg-slate-800 rounded-lg p-1 border border-white/5">
               <button
                  v-for="range in ['24H', '7D', '30D', '6M', '1Y']"
                  :key="range"
                  @click="setRange(range)"
                  class="px-3 py-1 text-xs rounded transition-all duration-200"
                  :class="selectedRange === range ? 'bg-blue-600 text-white shadow-lg' : 'text-slate-400 hover:text-white'"
               >
                  {{ range }}
               </button>

               <button
                  @click="showCustomDate = !showCustomDate"
                  class="px-3 py-1 text-xs rounded transition-all duration-200 ml-1"
                  :class="selectedRange === 'Custom' || showCustomDate ? 'bg-blue-600 text-white' : 'bg-slate-700 text-slate-300 hover:bg-slate-600'"
               >
                  📅
               </button>
            </div>
          </div>
        </div>

        <div v-if="showCustomDate" class="mb-6 p-4 bg-slate-800/50 rounded-2xl border border-white/10 animate-fade-in shadow-inner">
           <div class="grid grid-cols-2 gap-4">
              <div class="space-y-1.5">
                 <label class="text-[10px] text-blue-400 font-bold uppercase tracking-widest block ml-1">Start Point</label>
                 <input
                    v-model="startDate"
                    type="date"
                    class="w-full bg-slate-900 border border-slate-700 rounded-xl px-3 py-2 text-sm text-white focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 outline-none transition-all custom-calendar-icon"
                 >
              </div>
              <div class="space-y-1.5">
                 <label class="text-[10px] text-blue-400 font-bold uppercase tracking-widest block ml-1">End Point</label>
                 <input
                    v-model="endDate"
                    type="date"
                    class="w-full bg-slate-900 border border-slate-700 rounded-xl px-3 py-2 text-sm text-white focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 outline-none transition-all custom-calendar-icon"
                 >
              </div>
           </div>
           <button
              @click="applyCustomDate"
              class="w-full mt-4 bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold py-2.5 rounded-xl shadow-lg shadow-blue-900/20 transition-all active:scale-95"
           >
              UPDATE TIMELINE
           </button>
        </div>

        <div class="flex-1 bg-slate-900/50 rounded-2xl border border-white/5 p-4 relative flex flex-col justify-center">
          <EnergyChart :realData="historyData" :predictData="[]" :timeRange="selectedRange" />
        </div>
      </div>

    </div>
  </div>
</template>

<style scoped>
.animate-slide-in { animation: slideIn 0.4s cubic-bezier(0.16, 1, 0.3, 1); }
@keyframes slideIn { from { transform: translateX(100%); } to { transform: translateX(0); } }
.animate-fade-in { animation: fadeIn 0.3s ease-out; }
@keyframes fadeIn { from { opacity: 0; transform: translateY(-5px); } to { opacity: 1; transform: translateY(0); } }

/* ✅ แต่งสีไอคอนปฏิทินของ Browser ให้เป็นสีขาวเพื่อให้เข้ากับ Dark Mode */
.custom-calendar-icon::-webkit-calendar-picker-indicator {
    filter: invert(1);
    cursor: pointer;
    opacity: 0.6;
}
.custom-calendar-icon::-webkit-calendar-picker-indicator:hover {
    opacity: 1;
}
</style>

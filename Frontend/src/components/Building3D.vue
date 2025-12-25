<script setup>
import { ref } from 'vue'
import { useGLTF, Html } from '@tresjs/cientos'

// รับ Props (สำคัญคือ hideLabel)
const props = defineProps(['position', 'color', 'label', 'hideLabel'])
const model = ref(null)
const hovered = ref(false)

// โหลดโมเดล (ถ้ามีไฟล์ .glb)
try {
  const { scene } = await useGLTF('/building.glb')
  model.value = scene
} catch (e) {
  // ถ้าโหลดไม่ได้ จะไปใช้ TresBoxGeometry แทน
}
</script>

<template>
  <TresGroup
    :position="position"
    @pointer-enter="hovered = true"
    @pointer-leave="hovered = false"
  >
    <Html
      v-if="!hideLabel"
      :position="[0, 6, 0]"
      center
      transform
      :distance-factor="15"
      :z-index-range="[0, 0]"
    >
      <div
        class="px-3 py-1 rounded-full text-xs font-bold text-white transition-all duration-300 border border-white/20 backdrop-blur-md select-none pointer-events-none"
        :class="hovered ? 'bg-blue-600 scale-110 shadow-[0_0_15px_rgba(37,99,235,0.6)]' : 'bg-slate-900/80'"
      >
        {{ label }}
      </div>
    </Html>

    <primitive
      v-if="model"
      :object="model"
      :scale="[0.1, 0.1, 0.1]"
      :position="[0, 0, 0]"
    />

    <TresGroup v-else>
       <TresMesh :position="[0, 2.5, 0]" cast-shadow>
         <TresBoxGeometry :args="[3, 5, 3]" />
         <TresMeshStandardMaterial
            :color="hovered ? '#60a5fa' : color"
            :roughness="0.2"
            :metalness="0.8"
         />
       </TresMesh>

       <TresMesh :position="[0, 2.5, 0]">
         <TresBoxGeometry :args="[3.1, 5.1, 3.1]" />
         <TresMeshBasicMaterial color="#ffffff" wireframe :transparent="true" :opacity="0.1" />
       </TresMesh>
    </TresGroup>

    <TresMesh :position="[0, 0.1, 0]" receive-shadow>
       <TresCylinderGeometry :args="[2.5, 2.5, 0.2, 32]" />
       <TresMeshStandardMaterial color="#1e293b" />
    </TresMesh>

  </TresGroup>
</template>

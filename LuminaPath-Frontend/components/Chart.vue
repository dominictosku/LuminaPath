  <script>
    import { PolarArea } from 'vue-chartjs'
    import * as chartConfig from '~/utils/chartConfig.ts'
    import { Chart as ChartJS, Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, RadialLinearScale, ArcElement } from 'chart.js'
    
    ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, RadialLinearScale, ArcElement)
    
    export default {
      name: 'BarChart',
      components: { PolarArea },
      data() {
        return chartConfig
        
      }
    }
</script>

<template>
    <PolarArea
    :data="data"
    :options="options"
    />
</template>
  
  
  
  
  <script>
    import { Bar } from 'vue-chartjs'
    import * as chartConfig from '~/utils//Chart/chartConfig.ts'
    import { Chart as ChartJS, Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale } from 'chart.js'
    
    ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale)
    
    export default {
      name: 'BarChart',
      components: { Bar },
      data() {
        return chartConfig
        
      }
    }
</script>

<template>
    <Bar
    :data="data"
    :options="options"
    />
</template>
  
  
  
  
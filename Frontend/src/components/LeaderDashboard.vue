<template>
  <div id="leaderDashboardPage">

    <!-- 上部：产量指标卡片区 -->
    <div class="production-cards">
      <!-- 昨日产量卡片 -->
      <a-card 
        class="production-card" 
        :loading="isLoading"
        hoverable
      >
        <a-statistic
          title="昨日产量"
          :value="productionData.yesterday"
          :precision="2"
          suffix="kg"
          class="stat-item"
        >
          <template #prefix>
            <CalendarOutlined class="stat-icon" />
          </template>
        </a-statistic>
      </a-card>

      <!-- 当周产量卡片 -->
      <a-card 
        class="production-card" 
        :loading="isLoading"
        hoverable
      >
        <a-statistic
          title="当周产量"
          :value="productionData.week"
          :precision="2"
          suffix="kg"
          class="stat-item"
        >
          <template #prefix>
            <CalendarOutlined class="stat-icon" />
          </template>
        </a-statistic>
      </a-card>

      <!-- 当月产量卡片 -->
      <a-card 
        class="production-card" 
        :loading="isLoading"
        hoverable
      >
        <a-statistic
          title="当月产量"
          :value="productionData.month"
          :precision="2"
          suffix="kg"
          class="stat-item"
        >
          <template #prefix>
            <CalendarOutlined class="stat-icon" />
          </template>
        </a-statistic>
      </a-card>

      <!-- 今年产量卡片 -->
      <a-card 
        class="production-card" 
        :loading="isLoading"
        hoverable
      >
        <a-statistic
          title="今年产量"
          :value="productionData.year"
          :precision="2"
          suffix="kg"
          class="stat-item"
        >
          <template #prefix>
            <CalendarOutlined class="stat-icon" />
          </template>
        </a-statistic>
      </a-card>
    </div>

    <!-- 中部：折线图区域（三个趋势图） -->
    <div class="chart-section line-charts-section">

      <a-card 
        class="chart-card line-chart-card" 
        :loading="chartLoading.dayLine"
       
      >
        <!-- 移除手动下载按钮 -->
        <div style="width: 100%; height: 300px;">
          <div ref="dayLineChartRef" class="chart-container"></div>
        </div>
      </a-card>

      <a-card 
        class="chart-card line-chart-card" 
        :loading="chartLoading.weekLine"
      >
        <!-- 移除手动下载按钮 -->
        <div style="width: 100%; height: 300px;">
          <div ref="weekLineChartRef" class="chart-container"></div>
        </div>
      </a-card>

      <a-card 
        class="chart-card line-chart-card" 
        :loading="chartLoading.monthLine"
      >
        <!-- 移除手动下载按钮 -->
        <div style="width: 100%; height: 300px;">
          <div ref="monthLineChartRef" class="chart-container"></div>
        </div>
      </a-card>
    </div>
    
    <!-- 下部：饼图区域（产量占比） -->
    <div class="chart-section pie-chart-section">
      <a-card 
        class="chart-card" 
        :loading="chartLoading.pie"
      >
        <!-- 移除手动下载按钮 -->
        <div style="width: 100%; height: 300px;">
          <div ref="pieChartRef" class="chart-container"></div>
        </div>
      </a-card>
    </div>

    <!-- 数据刷新按钮 -->
    <div class="refresh-btn-group">
      <a-button 
        type="primary" 
        @click="fetchAllData"
        :loading="isLoading"
        icon="reload"
      >
        刷新全部数据
      </a-button>
    </div>
  </div>
</template>

<script lang="ts" setup>
// 1. 导入所有需要的生命周期和API
import { ref, reactive, onMounted, onUnmounted, watch, nextTick } from "vue";
import { message } from "ant-design-vue";
import { CalendarOutlined } from '@ant-design/icons-vue';
// 2. 确保ECharts引入正确（核心修复）
import * as echarts from 'echarts';

//接口
import{getLineChartOne,getLineChartTwo,getLineChartThree,getPieChart,getCoreChart} from'@/api/Dashboard'

// ===================== 类型定义 =====================
interface ProductionData {
  yesterday: number;
  week: number;
  month: number;
  year: number;
}

interface PieChartData {
  name: string;
  value: number;
}

interface LineChartData {
  xAxis: string[];
  series: {
    name: string;
    data: number[];
  }[];
}

interface ProductionQueryParams {
  factoryId?: string;
  warehouseId?: string;
  startTime?: string;
  endTime?: string;
}

// 适配真实接口的返回类型（code为0，message字段）
interface RealApiResponse<T> {
  code: number;
  data: T;
  message: string;
  description: string | null;
}

// 图表加载状态接口
interface ChartLoading {
  pie: boolean;
  dayLine: boolean;
  weekLine: boolean;
  monthLine: boolean;
}

// ===================== 状态管理 =====================
const isLoading = ref<boolean>(false); // 整体加载状态
const chartLoading = reactive<ChartLoading>({ // 各图表独立加载状态
  pie: false,
  dayLine: false,
  weekLine: false,
  monthLine: false
});

const productionData = reactive<ProductionData>({
  yesterday: 0,
  week: 0,
  month: 0,
  year: 0
});

// 图表DOM引用
const pieChartRef = ref<HTMLDivElement | null>(null);
const dayLineChartRef = ref<HTMLDivElement | null>(null);
const weekLineChartRef = ref<HTMLDivElement | null>(null);
const monthLineChartRef = ref<HTMLDivElement | null>(null);

// 图表实例
let pieChartInstance: echarts.ECharts | null = null;
let dayLineChartInstance: echarts.ECharts | null = null;
let weekLineChartInstance: echarts.ECharts | null = null;
let monthLineChartInstance: echarts.ECharts | null = null;

// 图表数据
const pieChartData = ref<PieChartData[]>([]);
const dayLineChartData = ref<LineChartData>({ xAxis: [], series: [] });
const weekLineChartData = ref<LineChartData>({ xAxis: [], series: [] });
const monthLineChartData = ref<LineChartData>({ xAxis: [], series: [] });

// ===================== 业务接口封装（独立调用版） =====================
// 获取核心产量数据
const fetchProductionData = async (params?: ProductionQueryParams) => {
  try {
    const axiosRes = await getCoreChart();
    const res = axiosRes.data as RealApiResponse<ProductionData>;
    // 适配真实接口的code判断（0表示成功）
    if (res.code === 0) {
      Object.assign(productionData, res.data);
    } else {
      throw new Error(res.message);
    }
  } catch (error) {
    console.error("获取核心产量数据失败：", error);
    message.error("核心产量数据加载失败");
  }
};

// 获取饼图数据并更新图表
const fetchPieChartData = async (params?: ProductionQueryParams) => {
  try {
    chartLoading.pie = true;
    const axiosRes = await getPieChart();
    const res = axiosRes.data as RealApiResponse<PieChartData[]>;
    if (res.code === 0) {
      pieChartData.value = res.data;
      // 数据更新后立即更新图表
      await nextTick();
      setTimeout(() => safeInitChart('pie'), 100);
    } else {
      throw new Error(res.message);
    }
  } catch (error) {
    console.error("获取饼图数据失败：", error);
    message.error("产量占比图表数据加载失败");
    // 空数据也初始化图表（避免实例为null）
    await nextTick();
    setTimeout(() => safeInitChart('pie'), 100);
  } finally {
    chartLoading.pie = false;
  }
};

// 获取日折线图数据并更新图表（适配真实接口返回格式）
const fetchDayLineChartData = async (params?: ProductionQueryParams) => {
  try {
    chartLoading.dayLine = true;
    // 调用实际的getLineChartThree接口
    const axiosRes = await getLineChartThree();
    const res = axiosRes.data as RealApiResponse<LineChartData>;
    // 核心修复：适配真实接口的判断逻辑
    if (res.code === 0) {
      // 安全赋值：先判断数据是否存在，避免undefined
      if (res.data && res.data.xAxis && res.data.series) {
        dayLineChartData.value = res.data;
      } else {
        throw new Error('接口返回数据格式异常');
      }
      // 数据更新后立即更新图表
      await nextTick();
      setTimeout(() => safeInitChart('dayLine'), 100);
    } else {
      throw new Error(res.message || '获取昨日时段产量数据失败');
    }
  } catch (error) {
    console.error("获取日折线图数据失败：", error);
    message.error("昨日时段产量趋势图表数据加载失败");
    // 空数据也初始化图表
    await nextTick();
    setTimeout(() => safeInitChart('dayLine'), 100);
  } finally {
    chartLoading.dayLine = false;
  }
};

// 获取周折线图数据并更新图表（修改：调用getLineChartTwo真实接口）
const fetchWeekLineChartData = async (params?: ProductionQueryParams) => {
  try {
    chartLoading.weekLine = true;
    // 调用实际的getLineChartTwo接口
    const axiosRes = await getLineChartTwo();
    const res = axiosRes.data as RealApiResponse<LineChartData>;
    // 适配真实接口的判断逻辑
    if (res.code === 0) {
      // 安全赋值：先判断数据是否存在，避免undefined
      if (res.data && res.data.xAxis && res.data.series) {
        weekLineChartData.value = res.data;
      } else {
        throw new Error('接口返回数据格式异常');
      }
      // 数据更新后立即更新图表
      await nextTick();
      setTimeout(() => safeInitChart('weekLine'), 100);
    } else {
      throw new Error(res.message || '获取周产量数据失败');
    }
  } catch (error) {
    console.error("获取周折线图数据失败：", error);
    message.error("周产量趋势图表数据加载失败");
    // 空数据也初始化图表
    await nextTick();
    setTimeout(() => safeInitChart('weekLine'), 100);
  } finally {
    chartLoading.weekLine = false;
  }
};

// 获取月折线图数据并更新图表（修改：调用getLineChartThree真实接口）
const fetchMonthLineChartData = async (params?: ProductionQueryParams) => {
  try {
    chartLoading.monthLine = true;
    // 调用实际的getLineChartThree接口
    const axiosRes = await getLineChartThree();
    const res = axiosRes.data as RealApiResponse<LineChartData>;
    // 适配真实接口的判断逻辑
    if (res.code === 0) {
      // 安全赋值：先判断数据是否存在，避免undefined
      if (res.data && res.data.xAxis && res.data.series) {
        monthLineChartData.value = res.data;
      } else {
        throw new Error('接口返回数据格式异常');
      }
      // 数据更新后立即更新图表
      await nextTick();
      setTimeout(() => safeInitChart('monthLine'), 100);
    } else {
      throw new Error(res.message || '获取月产量数据失败');
    }
  } catch (error) {
    console.error("获取月折线图数据失败：", error);
    message.error("月产量趋势图表数据加载失败");
    // 空数据也初始化图表
    await nextTick();
    setTimeout(() => safeInitChart('monthLine'), 100);
  } finally {
    chartLoading.monthLine = false;
  }
};

// ===================== 核心修复：图表初始化/更新（兼容空数据 + 内置下载） =====================
/**
 * 安全初始化/更新指定图表（兼容空数据）
 * @param chartType 图表类型：pie | dayLine | weekLine | monthLine | all
 */
const safeInitChart = (chartType: 'pie' | 'dayLine' | 'weekLine' | 'monthLine' | 'all' = 'all') => {
  // 初始化饼图（兼容空数据 + 内置下载 + 标题）
  if (chartType === 'pie' || chartType === 'all') {
    if (!pieChartRef.value) return;
    try {
      if (pieChartInstance) pieChartInstance.dispose();
      pieChartInstance = echarts.init(pieChartRef.value);
      // 空数据兜底
      const pieData = pieChartData.value.length ? pieChartData.value : [{ name: '暂无数据', value: 1 }];
      pieChartInstance.setOption({
        // 新增饼图标题配置
        title: {
          text: '占比',
          left: 'center',
          top: 10,
          textStyle: {
            fontSize: 16,
            fontWeight: 600,
            color: '#333'
          }
        },
        color: ['#003399', '#00AEEF', '#0066CC', '#66B2FF'],
        tooltip: { trigger: 'item', formatter: '{a} <br/>{b}: {c} 件 ({d}%)' },
        legend: { orient: 'horizontal', bottom: 0, textStyle: { color: '#333' } },
        toolbox: {
          show: true,
          feature: {
            saveAsImage: {
              show: true,
              title: '下载图片',
              type: 'png',
              pixelRatio: 2,
              backgroundColor: '#ffffff'
            }
          },
          right: 10,
          top: 10
        },
        series: [{
          name: '产量占比',
          type: 'pie',
          radius: ['40%', '70%'],
          avoidLabelOverlap: false,
          label: { show: false },
          emphasis: { label: { show: true, fontSize: 16, fontWeight: 600 } },
          labelLine: { show: false },
          data: pieData
        }]
      });
    } catch (error) {
      console.error("初始化饼图失败：", error);
      pieChartInstance = null;
    }
  }

  // 初始化日产量折线图（趋势1 + 标题）
  if (chartType === 'dayLine' || chartType === 'all') {
    if (!dayLineChartRef.value) return;
    try {
      if (dayLineChartInstance) dayLineChartInstance.dispose();
      dayLineChartInstance = echarts.init(dayLineChartRef.value);
      // 空数据兜底
      const xAxisData = dayLineChartData.value.xAxis.length ? dayLineChartData.value.xAxis : ['暂无数据'];
      const seriesData = dayLineChartData.value.series.length ? dayLineChartData.value.series : [{
        name: '产量',
        data: [0]
      }];
      dayLineChartInstance.setOption({
        // 新增日折线图标题配置（趋势1）
        title: {
          text: '趋势1',
          left: 'center',
          top: 10,
          textStyle: {
            fontSize: 16,
            fontWeight: 600,
            color: '#333'
          }
        },
        color: ['#003399'],
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        legend: { 
          orient: 'horizontal', 
          top: 40, // 调整legend位置，避免与标题重叠
          left: 'center',
          textStyle: { color: '#333', fontSize: 12 } 
        },
        toolbox: {
          show: true,
          feature: {
            saveAsImage: {
              show: true,
              title: '下载图片',
              type: 'png',
              pixelRatio: 2,
              backgroundColor: '#ffffff'
            }
          },
          right: 10,
          top: 10
        },
        grid: { left: '3%', right: '4%', bottom: '3%', top: '70px', containLabel: true }, // 调整top适配标题+legend
        xAxis: {
          type: 'category',
          data: xAxisData,
          axisLine: { lineStyle: { color: '#e8f4fc' } },
          axisLabel: { color: '#666' }
        },
        yAxis: {
          type: 'value',
          name: '产量(件)',
          nameTextStyle: { color: '#003399' },
          axisLine: { lineStyle: { color: '#e8f4fc' } },
          axisLabel: { color: '#666' },
          splitLine: { lineStyle: { color: '#e8f4fc' } }
        },
        series: seriesData.map(item => ({
          name: item.name,
          type: 'line',
          smooth: true,
          data: item.data,
          lineStyle: { width: 2 },
          itemStyle: { color: '#00AEEF', borderColor: '#003399', borderWidth: 2 },
          areaStyle: {
            color: {
              type: 'linear',
              x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [
                { offset: 0, color: 'rgba(0, 174, 239, 0.2)' },
                { offset: 0, color: 'rgba(0, 174, 239, 0.05)' }
              ]
            }
          }
        }))
      });
    } catch (error) {
      console.error("初始化日折线图失败：", error);
      dayLineChartInstance = null;
    }
  }

  // 初始化周产量折线图（趋势2 + 标题）
  if (chartType === 'weekLine' || chartType === 'all') {
    if (!weekLineChartRef.value) return;
    try {
      if (weekLineChartInstance) weekLineChartInstance.dispose();
      weekLineChartInstance = echarts.init(weekLineChartRef.value);
      // 空数据兜底
      const xAxisData = weekLineChartData.value.xAxis.length ? weekLineChartData.value.xAxis : ['暂无数据'];
      const seriesData = weekLineChartData.value.series.length ? weekLineChartData.value.series : [{
        name: '产量',
        data: [0]
      }];
      weekLineChartInstance.setOption({
        // 新增周折线图标题配置（趋势2）
        title: {
          text: '趋势2',
          left: 'center',
          top: 10,
          textStyle: {
            fontSize: 16,
            fontWeight: 600,
            color: '#333'
          }
        },
        color: ['#003399'],
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        legend: { 
          orient: 'horizontal', 
          top: 40, // 调整legend位置，避免与标题重叠
          left: 'center',
          textStyle: { color: '#333', fontSize: 12 } 
        },
        toolbox: {
          show: true,
          feature: {
            saveAsImage: {
              show: true,
              title: '下载图片',
              type: 'png',
              pixelRatio: 2,
              backgroundColor: '#ffffff'
            }
          },
          right: 10,
          top: 10
        },
        grid: { left: '3%', right: '4%', bottom: '3%', top: '70px', containLabel: true }, // 调整top适配标题+legend
        xAxis: {
          type: 'category',
          data: xAxisData,
          axisLine: { lineStyle: { color: '#e8f4fc' } },
          axisLabel: { color: '#666' }
        },
        yAxis: {
          type: 'value',
          name: '产量(件)',
          nameTextStyle: { color: '#003399' },
          axisLine: { lineStyle: { color: '#e8f4fc' } },
          axisLabel: { color: '#666' },
          splitLine: { lineStyle: { color: '#e8f4fc' } }
        },
        series: seriesData.map(item => ({
          name: item.name,
          type: 'line',
          smooth: true,
          data: item.data,
          lineStyle: { width: 2 },
          itemStyle: { color: '#00AEEF', borderColor: '#003399', borderWidth: 2 },
          areaStyle: {
            color: {
              type: 'linear',
              x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [
                { offset: 0, color: 'rgba(0, 174, 239, 0.2)' },
                { offset: 0, color: 'rgba(0, 174, 239, 0.05)' }
              ]
            }
          }
        }))
      });
    } catch (error) {
      console.error("初始化周折线图失败：", error);
      weekLineChartInstance = null;
    }
  }

  // 初始化月产量折线图（趋势3 + 标题）
  if (chartType === 'monthLine' || chartType === 'all') {
    if (!monthLineChartRef.value) return;
    try {
      if (monthLineChartInstance) monthLineChartInstance.dispose();
      monthLineChartInstance = echarts.init(monthLineChartRef.value);
      // 空数据兜底
      const xAxisData = monthLineChartData.value.xAxis.length ? monthLineChartData.value.xAxis : ['暂无数据'];
      const seriesData = monthLineChartData.value.series.length ? monthLineChartData.value.series : [{
        name: '产量',
        data: [0]
      }];
      monthLineChartInstance.setOption({
        // 新增月折线图标题配置（趋势3）
        title: {
          text: '趋势3',
          left: 'center',
          top: 10,
          textStyle: {
            fontSize: 16,
            fontWeight: 600,
            color: '#333'
          }
        },
        color: ['#003399'],
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        legend: { 
          orient: 'horizontal', 
          top: 40, // 调整legend位置，避免与标题重叠
          left: 'center',
          textStyle: { color: '#333', fontSize: 12 } 
        },
        toolbox: {
          show: true,
          feature: {
            saveAsImage: {
              show: true,
              title: '下载图片',
              type: 'png',
              pixelRatio: 2,
              backgroundColor: '#ffffff'
            }
          },
          right: 10,
          top: 10
        },
        grid: { left: '3%', right: '4%', bottom: '3%', top: '70px', containLabel: true }, // 调整top适配标题+legend
        xAxis: {
          type: 'category',
          data: xAxisData,
          axisLine: { lineStyle: { color: '#e8f4fc' } },
          axisLabel: { color: '#666' }
        },
        yAxis: {
          type: 'value',
          name: '产量(件)',
          nameTextStyle: { color: '#003399' },
          axisLine: { lineStyle: { color: '#e8f4fc' } },
          axisLabel: { color: '#666' },
          splitLine: { lineStyle: { color: '#e8f4fc' } }
        },
        series: seriesData.map(item => ({
          name: item.name,
          type: 'line',
          smooth: true,
          data: item.data,
          lineStyle: { width: 2 },
          itemStyle: { color: '#00AEEF', borderColor: '#003399', borderWidth: 2 },
          areaStyle: {
            color: {
              type: 'linear',
              x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [
                { offset: 0, color: 'rgba(0, 174, 239, 0.2)' },
                { offset: 0, color: 'rgba(0, 174, 239, 0.05)' }
              ]
            }
          }
        }))
      });
    } catch (error) {
      console.error("初始化月折线图失败：", error);
      monthLineChartInstance = null;
    }
  }
};

// ===================== 数据刷新入口 =====================
const fetchAllData = async () => {
  try {
    isLoading.value = true;
    const params: ProductionQueryParams = {};

    await fetchProductionData(params);
    await Promise.all([
      fetchPieChartData(params),
      fetchDayLineChartData(params),
      fetchWeekLineChartData(params),
      fetchMonthLineChartData(params)
    ]);

    message.success("数据刷新请求已发送");
    
    // 延迟初始化所有图表（确保数据和DOM都就绪）
    setTimeout(() => {
      safeInitChart('all');
    }, 500);
  } catch (error) {
    console.error("获取核心数据失败：", error);
    message.error("核心数据加载失败，请稍后重试");
  } finally {
    setTimeout(() => {
      isLoading.value = false;
    }, 600);
  }
};

// ===================== 生命周期 =====================
// 页面挂载时加载数据
onMounted(async () => {
  await fetchAllData();
  
  // 监听窗口大小变化，自适应图表
  const resizeHandler = () => {
    if (pieChartInstance) pieChartInstance.resize();
    if (dayLineChartInstance) dayLineChartInstance.resize();
    if (weekLineChartInstance) weekLineChartInstance.resize();
    if (monthLineChartInstance) monthLineChartInstance.resize();
  };
  window.addEventListener('resize', resizeHandler);
  
  // 组件卸载时移除监听
  onUnmounted(() => {
    window.removeEventListener('resize', resizeHandler);
    // 销毁图表实例，避免内存泄漏
    if (pieChartInstance) pieChartInstance.dispose();
    if (dayLineChartInstance) dayLineChartInstance.dispose();
    if (weekLineChartInstance) weekLineChartInstance.dispose();
    if (monthLineChartInstance) monthLineChartInstance.dispose();
  });
});
</script>

<style scoped>
#leaderDashboardPage {
  padding: 20px;
  background-color: #f5f7fa;
  min-height: 100vh;
}

.production-cards {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
}

.production-card {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.stat-item {
  padding: 8px 0;
}

.stat-icon {
  color: #003399;
  font-size: 18px;
}

.chart-section {
  margin-bottom: 20px;
}

.line-charts-section {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
}

.chart-card {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.line-chart-card {
  height: 380px;
}

.chart-container {
  width: 100%;
  height: 100%;
}

.refresh-btn-group {
  margin-top: 20px;
  text-align: right;
}

.pie-chart-section {
  height: 400px;
}

@media (max-width: 1200px) {
  .line-charts-section {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .production-cards {
    grid-template-columns: repeat(2, 1fr);
  }
  .line-charts-section {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 480px) {
  .production-cards {
    grid-template-columns: 1fr;
  }
}
</style>
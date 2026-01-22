<template>
  <div id="leaderDashboardPage">
    <!-- 顶部：产量核心指标卡片 -->
    <div class="page-header">
      <h2 class="page-title">数据看板</h2>
      <p class="page-desc">展示核心指标及趋势数据</p>
    </div>

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
          :precision="0"
          suffix="件"
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
          :precision="0"
          suffix="件"
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
          :precision="0"
          suffix="件"
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
          :precision="0"
          suffix="件"
          class="stat-item"
        >
          <template #prefix>
            <CalendarOutlined class="stat-icon" />
          </template>
        </a-statistic>
      </a-card>
    </div>

    <!-- 中部：饼图区域（产量占比） -->
    <div class="chart-section pie-chart-section">
      <a-card 
        class="chart-card" 
        :loading="isLoading"
        title="产量占比分析"
        :title-style="{ color: '#003399', fontWeight: 600 }"
      >
        <!-- 修复：添加固定高度的父容器，确保DOM稳定 -->
        <div style="width: 100%; height: 300px;">
          <div ref="pieChartRef" class="chart-container"></div>
        </div>
      </a-card>
    </div>

    <!-- 下部：折线图区域（三个趋势图） -->
    <div class="chart-section line-charts-section">
      <!-- 日产量趋势 -->
      <a-card 
        class="chart-card line-chart-card" 
        :loading="isLoading"
        title="日产量趋势"
        :title-style="{ color: '#003399', fontWeight: 600 }"
      >
        <div style="width: 100%; height: 300px;">
          <div ref="dayLineChartRef" class="chart-container"></div>
        </div>
      </a-card>

      <!-- 周产量趋势 -->
      <a-card 
        class="chart-card line-chart-card" 
        :loading="isLoading"
        title="周产量趋势"
        :title-style="{ color: '#003399', fontWeight: 600 }"
      >
        <div style="width: 100%; height: 300px;">
          <div ref="weekLineChartRef" class="chart-container"></div>
        </div>
      </a-card>

      <!-- 月产量趋势 -->
      <a-card 
        class="chart-card line-chart-card" 
        :loading="isLoading"
        title="月产量趋势"
        :title-style="{ color: '#003399', fontWeight: 600 }"
      >
        <div style="width: 100%; height: 300px;">
          <div ref="monthLineChartRef" class="chart-container"></div>
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
import { ref, reactive, onMounted, onUnmounted, watch, nextTick, onUpdated } from "vue";
import { message } from "ant-design-vue";
import { CalendarOutlined } from '@ant-design/icons-vue';
import { Card, Statistic, Button } from 'ant-design-vue';

// 2. 确保ECharts引入正确（核心修复）
import * as echarts from 'echarts';

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

interface ApiResponse<T> {
  code: number;
  msg: string;
  data: T;
}

// ===================== 状态管理 =====================
const isLoading = ref<boolean>(false);

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

// 观察者实例（用于监听DOM可见性）
let chartObserver: IntersectionObserver | null = null;

// 图表数据
const pieChartData = ref<PieChartData[]>([]);
const dayLineChartData = ref<LineChartData>({ xAxis: [], series: [] });
const weekLineChartData = ref<LineChartData>({ xAxis: [], series: [] });
const monthLineChartData = ref<LineChartData>({ xAxis: [], series: [] });

// ===================== 统一请求封装 =====================
const requestApi = async <T>(url: string, params?: ProductionQueryParams): Promise<ApiResponse<T>> => {
  // 模拟接口返回
  const mockApiMap: Record<string, () => Promise<ApiResponse<T>>> = {
    '/api/production/core': async () => ({
      code: 200,
      msg: 'success',
      data: {
        yesterday: Math.floor(Math.random() * 2000 + 12000),
        week: Math.floor(Math.random() * 5000 + 85000),
        month: Math.floor(Math.random() * 10000 + 380000),
        year: Math.floor(Math.random() * 50000 + 4200000)
      } as T
    }),
    '/api/production/pie': async () => ({
      code: 200,
      msg: 'success',
      data: [
        { name: '车间A', value: Math.floor(Math.random() * 10000 + 180000) },
        { name: '车间B', value: Math.floor(Math.random() * 8000 + 120000) },
        { name: '车间C', value: Math.floor(Math.random() * 5000 + 70000) },
        { name: '外协加工', value: Math.floor(Math.random() * 3000 + 40000) }
      ] as T
    }),
    '/api/production/dayLine': async () => ({
      code: 200,
      msg: 'success',
      data: {
        xAxis: ['01日', '02日', '03日', '04日', '05日', '06日', '07日'],
        series: [{
          name: '日产量',
          data: [11800, 12580, 13200, 11950, 12800, 12100, 12580].map(num => num + Math.floor(Math.random() * 500 - 250))
        }]
      } as T
    }),
    '/api/production/weekLine': async () => ({
      code: 200,
      msg: 'success',
      data: {
        xAxis: ['第1周', '第2周', '第3周', '第4周', '第5周'],
        series: [{
          name: '周产量',
          data: [78500, 82600, 85800, 89650, 87200].map(num => num + Math.floor(Math.random() * 1000 - 500))
        }]
      } as T
    }),
    '/api/production/monthLine': async () => ({
      code: 200,
      msg: 'success',
      data: {
        xAxis: ['1月', '2月', '3月', '4月', '5月', '6月', '7月', '8月', '9月', '10月', '11月', '12月'],
        series: [{
          name: '月产量',
          data: [325000, 348000, 362000, 375000, 380000, 385600, 392000, 398000, 405000, 410000, 415000, 420000]
            .map(num => num + Math.floor(Math.random() * 2000 - 1000))
        }]
      } as T
    })
  };

  if (mockApiMap[url]) {
    await new Promise(resolve => setTimeout(resolve, 500));
    return mockApiMap[url]();
  }

  throw new Error(`未找到模拟接口: ${url}`);
};

// ===================== 业务接口封装 =====================
const getProductionData = async (params?: ProductionQueryParams): Promise<ProductionData> => {
  const res = await requestApi<ProductionData>('/api/production/core', params);
  if (res.code !== 200) throw new Error(res.msg);
  return res.data;
};

const getPieChartData = async (params?: ProductionQueryParams): Promise<PieChartData[]> => {
  const res = await requestApi<PieChartData[]>('/api/production/pie', params);
  if (res.code !== 200) throw new Error(res.msg);
  return res.data;
};

const getDayLineChartData = async (params?: ProductionQueryParams): Promise<LineChartData> => {
  const res = await requestApi<LineChartData>('/api/production/dayLine', params);
  if (res.code !== 200) throw new Error(res.msg);
  return res.data;
};

const getWeekLineChartData = async (params?: ProductionQueryParams): Promise<LineChartData> => {
  const res = await requestApi<LineChartData>('/api/production/weekLine', params);
  if (res.code !== 200) throw new Error(res.msg);
  return res.data;
};

const getMonthLineChartData = async (params?: ProductionQueryParams): Promise<LineChartData> => {
  const res = await requestApi<LineChartData>('/api/production/monthLine', params);
  if (res.code !== 200) throw new Error(res.msg);
  return res.data;
};

// ===================== 图表初始化/更新（终极修复版） =====================
/**
 * 安全初始化图表（添加多重校验）
 */
const safeInitChart = () => {
  // 前置校验：确保所有DOM和数据都已就绪
  if (!pieChartRef.value || !dayLineChartRef.value || !weekLineChartRef.value || !monthLineChartRef.value) {
    console.warn('【图表初始化】DOM容器未就绪，跳过本次初始化');
    return;
  }

  if (pieChartData.value.length === 0 || dayLineChartData.value.series.length === 0) {
    console.warn('【图表初始化】数据未就绪，跳过本次初始化');
    return;
  }

  try {
    // 初始化饼图
    if (pieChartInstance) pieChartInstance.dispose();
    pieChartInstance = echarts.init(pieChartRef.value);
    pieChartInstance.setOption({
      color: ['#003399', '#00AEEF', '#0066CC', '#66B2FF'],
      tooltip: { trigger: 'item', formatter: '{a} <br/>{b}: {c} 件 ({d}%)' },
      legend: { orient: 'horizontal', bottom: 0, textStyle: { color: '#333' } },
      series: [{
        name: '产量占比',
        type: 'pie',
        radius: ['40%', '70%'],
        avoidLabelOverlap: false,
        label: { show: false },
        emphasis: { label: { show: true, fontSize: 16, fontWeight: 600 } },
        labelLine: { show: false },
        data: pieChartData.value
      }]
    });

    // 初始化日产量折线图
    if (dayLineChartInstance) dayLineChartInstance.dispose();
    dayLineChartInstance = echarts.init(dayLineChartRef.value);
    dayLineChartInstance.setOption({
      color: ['#003399'],
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
      xAxis: {
        type: 'category',
        data: dayLineChartData.value.xAxis,
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
      series: dayLineChartData.value.series.map(item => ({
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
              { offset: 1, color: 'rgba(0, 174, 239, 0.05)' }
            ]
          }
        }
      }))
    });

    // 初始化周产量折线图
    if (weekLineChartInstance) weekLineChartInstance.dispose();
    weekLineChartInstance = echarts.init(weekLineChartRef.value);
    weekLineChartInstance.setOption({
      color: ['#003399'],
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
      xAxis: {
        type: 'category',
        data: weekLineChartData.value.xAxis,
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
      series: weekLineChartData.value.series.map(item => ({
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
              { offset: 1, color: 'rgba(0, 174, 239, 0.05)' }
            ]
          }
        }
      }))
    });

    // 初始化月产量折线图
    if (monthLineChartInstance) monthLineChartInstance.dispose();
    monthLineChartInstance = echarts.init(monthLineChartRef.value);
    monthLineChartInstance.setOption({
      color: ['#003399'],
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
      xAxis: {
        type: 'category',
        data: monthLineChartData.value.xAxis,
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
      series: monthLineChartData.value.series.map(item => ({
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
              { offset: 0, color: 'rgba(0, 174, 239, 0.2)' }, // 修复：之前重复写了offset:0
              { offset: 1, color: 'rgba(0, 174, 239, 0.05)' }
            ]
          }
        }
      }))
    });

    console.log('【图表初始化】所有图表初始化成功');
  } catch (error) {
    console.error('【图表初始化】失败:', error);
  }
};

/**
 * 窗口resize时更新图表尺寸
 */
const resizeCharts = () => {
  pieChartInstance?.resize();
  dayLineChartInstance?.resize();
  weekLineChartInstance?.resize();
  monthLineChartInstance?.resize();
};

// ===================== 业务方法（多重保障版） =====================
const fetchAllData = async () => {
  try {
    isLoading.value = true;
    const params: ProductionQueryParams = {};

    // 1. 请求数据
    const [prodData, pieData, dayLineData, weekLineData, monthLineData] = await Promise.all([
      getProductionData(params),
      getPieChartData(params),
      getDayLineChartData(params),
      getWeekLineChartData(params),
      getMonthLineChartData(params)
    ]);

    // 2. 更新数据
    Object.assign(productionData, prodData);
    pieChartData.value = pieData;
    dayLineChartData.value = dayLineData;
    weekLineChartData.value = weekLineData;
    monthLineChartData.value = monthLineData;

    // 3. 三重保障：nextTick + 延迟 + 安全初始化
    await nextTick(); // 等待Vue DOM更新
    setTimeout(() => { // 等待Ant Design组件渲染
      safeInitChart(); // 安全初始化图表
    }, 300);

    message.success("全部数据刷新成功");
  } catch (error) {
    console.error("获取数据失败：", error);
    message.error("数据加载失败，请稍后重试");
  } finally {
    isLoading.value = false;
  }
};

// ===================== 生命周期（完整监听） =====================
onMounted(async () => {
  // 1. 加载数据
  await fetchAllData();

  // 2. 监听窗口resize
  window.addEventListener('resize', resizeCharts);

  // 3. 监听DOM可见性（终极保障）
  chartObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        console.log('【DOM监听】图表容器可见，触发初始化');
        safeInitChart();
      }
    });
  }, { threshold: 0.1 });

  // 4. 监听所有图表容器
  if (pieChartRef.value) chartObserver.observe(pieChartRef.value);
  if (dayLineChartRef.value) chartObserver.observe(dayLineChartRef.value);
  if (weekLineChartRef.value) chartObserver.observe(weekLineChartRef.value);
  if (monthLineChartRef.value) chartObserver.observe(monthLineChartRef.value);
});

// 5. 监听组件DOM更新（补充保障）
onUpdated(async () => {
  await nextTick();
  safeInitChart();
});

onUnmounted(() => {
  // 1. 销毁图表实例
  pieChartInstance?.dispose();
  dayLineChartInstance?.dispose();
  weekLineChartInstance?.dispose();
  monthLineChartInstance?.dispose();

  // 2. 移除resize监听
  window.removeEventListener('resize', resizeCharts);

  // 3. 销毁观察者
  if (chartObserver) {
    chartObserver.disconnect();
    chartObserver = null;
  }
});

// 6. 监听数据变化，自动重新渲染（修复：monthLineData → monthLineChartData）
watch([pieChartData, dayLineChartData, weekLineChartData, monthLineChartData], async () => {
  await nextTick();
  setTimeout(() => {
    safeInitChart();
  }, 100);
}, { deep: true });
</script>

<style scoped>
#leaderDashboardPage {
  padding: 20px;
  background-color: #f9fbfd;
  min-height: calc(100vh - 80px);
}

.page-header {
  margin-bottom: 24px;
}

.page-title {
  margin: 0 0 8px 0;
  font-size: 24px;
  font-weight: 600;
  color: #003399;
}

.page-desc {
  margin: 0;
  color: #666;
  font-size: 14px;
}

.production-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 20px;
  margin-bottom: 24px;
}

.production-card {
  border: 1px solid #e8f4fc;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 51, 153, 0.06);
  transition: all 0.3s ease;
}

.production-card:hover {
  box-shadow: 0 4px 16px rgba(0, 174, 239, 0.12);
  border-color: #00AEEF;
  transform: translateY(-2px);
}

.stat-item {
  --ant-statistic-title-color: #333;
  --ant-statistic-content-color: #003399;
}

.stat-icon {
  color: #00AEEF;
  font-size: 20px;
}

.chart-section {
  margin-bottom: 24px;
  width: 100%;
}

.pie-chart-section .chart-card {
  border: 1px solid #e8f4fc;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 51, 153, 0.06);
}

.line-charts-section {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 20px;
}

.line-chart-card {
  border: 1px solid #e8f4fc;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 51, 153, 0.06);
  transition: all 0.3s ease;
}

.line-chart-card:hover {
  box-shadow: 0 4px 16px rgba(0, 174, 239, 0.12);
  border-color: #00AEEF;
}

/* 强制确保图表容器尺寸 */
.chart-container {
  width: 100% !important;
  height: 100% !important;
  min-height: 300px !important;
}

.refresh-btn-group {
  display: flex;
  justify-content: flex-end;
}

::v-deep .ant-btn-primary {
  background: #003399;
  border-color: #003399;
}

::v-deep .ant-btn-primary:hover,
::v-deep .ant-btn-primary:focus {
  background: #0066CC;
  border-color: #0066CC;
}

/* 响应式适配 */
@media (max-width: 1200px) {
  .line-charts-section {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .production-cards {
    grid-template-columns: 1fr;
  }

  .line-charts-section {
    grid-template-columns: 1fr;
  }

  .page-title {
    font-size: 20px;
  }

  .chart-container {
    height: 250px !important;
    min-height: 250px !important;
  }
}
</style>
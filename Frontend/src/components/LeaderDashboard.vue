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
        :loading="chartLoading.pie"
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
        :loading="chartLoading.dayLine"
        title="昨日时段产量趋势"
        :title-style="{ color: '#003399', fontWeight: 600 }"
      >
        <div style="width: 100%; height: 300px;">
          <div ref="dayLineChartRef" class="chart-container"></div>
        </div>
      </a-card>

      <!-- 周产量趋势 -->
      <a-card 
        class="chart-card line-chart-card" 
        :loading="chartLoading.weekLine"
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
        :loading="chartLoading.monthLine"
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
//接口
import{getLineChartOne,getLineChartTwo,getLineChartThree} from'@/api/Dashboard'
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

// 观察者实例（用于监听DOM可见性）
let chartObserver: IntersectionObserver | null = null;

// 图表数据
const pieChartData = ref<PieChartData[]>([]);
const dayLineChartData = ref<LineChartData>({ xAxis: [], series: [] });
const weekLineChartData = ref<LineChartData>({ xAxis: [], series: [] });
const monthLineChartData = ref<LineChartData>({ xAxis: [], series: [] });

// ===================== 统一请求封装 =====================
const requestApi = async <T>(url: string, params?: ProductionQueryParams): Promise<RealApiResponse<T>> => {
  // 模拟接口返回（适配真实接口格式：code=0，message字段）
  const mockApiMap: Record<string, () => Promise<RealApiResponse<T>>> = {
    '/api/production/core': async () => ({
      code: 0,
      message: 'ok',
      description: null,
      data: {
        yesterday: Math.floor(Math.random() * 2000 + 12000),
        week: Math.floor(Math.random() * 5000 + 85000),
        month: Math.floor(Math.random() * 10000 + 380000),
        year: Math.floor(Math.random() * 50000 + 4200000)
      } as T
    }),
    '/api/production/pie': async () => ({
      code: 0,
      message: 'ok',
      description: null,
      data: [
        { name: '车间A', value: Math.floor(Math.random() * 10000 + 180000) },
        { name: '车间B', value: Math.floor(Math.random() * 8000 + 120000) },
        { name: '车间C', value: Math.floor(Math.random() * 5000 + 70000) },
        { name: '外协加工', value: Math.floor(Math.random() * 3000 + 40000) }
      ] as T
    })
  };

  if (mockApiMap[url]) {
    await new Promise(resolve => setTimeout(resolve, 500));
    return mockApiMap[url]();
  }

  throw new Error(`未找到模拟接口: ${url}`);
};

// ===================== 业务接口封装（独立调用版） =====================
// 获取核心产量数据
const fetchProductionData = async (params?: ProductionQueryParams) => {
  try {
    const res = await requestApi<ProductionData>('/api/production/core', params);
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
    const res = await requestApi<PieChartData[]>('/api/production/pie', params);
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
  } finally {
    chartLoading.pie = false;
  }
};

// 获取日折线图数据并更新图表（适配真实接口返回格式）
const fetchDayLineChartData = async (params?: ProductionQueryParams) => {
  try {
    chartLoading.dayLine = true;
    // 调用实际的getLineChartOne接口
    const axiosRes = await getLineChartOne();
    
    const res = axiosRes.data as RealApiResponse<LineChartData>;
    // 核心修复：适配真实接口的判断逻辑
    // 1. 判断code为0（而非200）
    // 2. 读取message（而非msg）
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
    await nextTick();
    setTimeout(() => safeInitChart('monthLine'), 100);
  } finally {
    chartLoading.monthLine = false;
  }
};

// ===================== 图表初始化/更新（独立更新版） =====================
/**
 * 安全初始化/更新指定图表
 * @param chartType 图表类型：pie | dayLine | weekLine | monthLine | all
 */
const safeInitChart = (chartType: 'pie' | 'dayLine' | 'weekLine' | 'monthLine' | 'all' = 'all') => {
  // 初始化饼图
  if (chartType === 'pie' || chartType === 'all') {
    if (!pieChartRef.value || !pieChartData.value || pieChartData.value.length === 0) return;
    try {
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
    } catch (error) {
      console.error("初始化饼图失败：", error);
    }
  }

  // 初始化日产量折线图（适配时段数据）
  if (chartType === 'dayLine' || chartType === 'all') {
    // 核心修复：增加多层判空，避免读取undefined的length
    if (!dayLineChartRef.value || !dayLineChartData.value || !dayLineChartData.value.xAxis || !dayLineChartData.value.series || dayLineChartData.value.series.length === 0) return;
    try {
      if (dayLineChartInstance) dayLineChartInstance.dispose();
      dayLineChartInstance = echarts.init(dayLineChartRef.value);
      dayLineChartInstance.setOption({
        color: ['#003399'],
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        // 新增：显示图例
        legend: { 
          orient: 'horizontal', 
          top: 0, 
          left: 'center',
          textStyle: { color: '#333', fontSize: 12 } 
        },
        grid: { left: '3%', right: '4%', bottom: '3%', top: '10%', containLabel: true },
        xAxis: {
          type: 'category',
          data: dayLineChartData.value.xAxis,
          axisLine: { lineStyle: { color: '#e8f4fc' } },
          axisLabel: { color: '#666' } // 旋转x轴标签，避免时段文字重叠
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
    } catch (error) {
      console.error("初始化日折线图失败：", error);
    }
  }

  // 初始化周产量折线图
  if (chartType === 'weekLine' || chartType === 'all') {
    if (!weekLineChartRef.value || !weekLineChartData.value || !weekLineChartData.value.series || weekLineChartData.value.series.length === 0) return;
    try {
      if (weekLineChartInstance) weekLineChartInstance.dispose();
      weekLineChartInstance = echarts.init(weekLineChartRef.value);
      weekLineChartInstance.setOption({
        color: ['#003399'],
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        // 新增：显示图例
        legend: { 
          orient: 'horizontal', 
          top: 0, 
          left: 'center',
          textStyle: { color: '#333', fontSize: 12 } 
        },
        grid: { left: '3%', right: '4%', bottom: '3%', top: '10%', containLabel: true },
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
    } catch (error) {
      console.error("初始化周折线图失败：", error);
    }
  }

  // 初始化月产量折线图
  if (chartType === 'monthLine' || chartType === 'all') {
    if (!monthLineChartRef.value || !monthLineChartData.value || !monthLineChartData.value.series || monthLineChartData.value.series.length === 0) return;
    try {
      if (monthLineChartInstance) monthLineChartInstance.dispose();
      monthLineChartInstance = echarts.init(monthLineChartRef.value);
      monthLineChartInstance.setOption({
        // 修改：增加多颜色支持，适配两条折线
        color: ['#003399', '#00AEEF'],
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        // 新增：显示图例
        legend: { 
          orient: 'horizontal', 
          top: 0, 
          left: 'center',
          textStyle: { color: '#333', fontSize: 12 } 
        },
        grid: { left: '3%', right: '4%', bottom: '3%', top: '10%', containLabel: true },
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
        series: monthLineChartData.value.series.map((item, index) => ({
          name: item.name,
          type: 'line',
          smooth: true,
          data: item.data,
          lineStyle: { width: 2 },
          // 修改：根据索引设置不同的颜色，区分两条折线
          itemStyle: { 
            color: index === 0 ? '#003399' : '#00AEEF', 
            borderColor: index === 0 ? '#0066CC' : '#66B2FF', 
            borderWidth: 2 
          },
          areaStyle: {
            color: {
              type: 'linear',
              x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [
                { offset: 0, color: index === 0 ? 'rgba(0, 51, 153, 0.2)' : 'rgba(0, 174, 239, 0.2)' },
                { offset: 1, color: index === 0 ? 'rgba(0, 51, 153, 0.05)' : 'rgba(0, 174, 239, 0.05)' }
              ]
            }
          }
        }))
      });
    } catch (error) {
      console.error("初始化月折线图失败：", error);
    }
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

// ===================== 业务方法（独立调用版） =====================
const fetchAllData = async () => {
  try {
    isLoading.value = true;
    const params: ProductionQueryParams = {};

    // 1. 独立调用各接口，互不影响
    await fetchProductionData(params); // 核心产量数据
    fetchPieChartData(params); // 饼图数据（异步无等待，独立执行）
    fetchDayLineChartData(params); // 日折线图数据（异步无等待）
    fetchWeekLineChartData(params); // 周折线图数据（异步无等待）
    fetchMonthLineChartData(params); // 月折线图数据（异步无等待）

    message.success("数据刷新请求已发送");
  } catch (error) {
    console.error("获取核心数据失败：", error);
    message.error("核心数据加载失败，请稍后重试");
  } finally {
    setTimeout(() => {
      isLoading.value = false; // 整体加载状态延迟关闭
    }, 600);
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
        safeInitChart('all');
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
  safeInitChart('all');
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

// 6. 监听单个图表数据变化，只更新对应图表（增加判空）
watch(pieChartData, async () => {
  await nextTick();
  safeInitChart('pie');
}, { deep: true });

watch(dayLineChartData, async () => {
  await nextTick();
  safeInitChart('dayLine');
}, { deep: true });

watch(weekLineChartData, async () => {
  await nextTick();
  safeInitChart('weekLine');
}, { deep: true });

watch(monthLineChartData, async () => {
  await nextTick();
  safeInitChart('monthLine');
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
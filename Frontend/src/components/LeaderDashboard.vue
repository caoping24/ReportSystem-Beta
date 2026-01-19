<template>
  <div id="leaderDashboardPage">
    <!-- 顶部：产量核心指标卡片 -->
    <div class="page-header">
      <h2 class="page-title">领导仓数据看板</h2>
      <p class="page-desc">实时展示产量核心指标及趋势数据</p>
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
        <div ref="pieChartRef" class="chart-container"></div>
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
        <div ref="dayLineChartRef" class="chart-container"></div>
      </a-card>

      <!-- 周产量趋势 -->
      <a-card 
        class="chart-card line-chart-card" 
        :loading="isLoading"
        title="周产量趋势"
        :title-style="{ color: '#003399', fontWeight: 600 }"
      >
        <div ref="weekLineChartRef" class="chart-container"></div>
      </a-card>

      <!-- 月产量趋势 -->
      <a-card 
        class="chart-card line-chart-card" 
        :loading="isLoading"
        title="月产量趋势"
        :title-style="{ color: '#003399', fontWeight: 600 }"
      >
        <div ref="monthLineChartRef" class="chart-container"></div>
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
import { ref, reactive, onMounted, onUnmounted, watch } from "vue";
import { message } from "ant-design-vue";
import { CalendarOutlined } from '@ant-design/icons-vue';
import { Card, Statistic, Button } from 'ant-design-vue';
// 引入ECharts核心及图表类型

import echarts from "../store/echarts";
// 注册ECharts组件

// ===================== 类型定义（预留接口对接） =====================
// 产量核心指标类型
interface ProductionData {
  yesterday: number; // 昨日产量
  week: number;      // 当周产量
  month: number;     // 当月产量
  year: number;      // 今年产量
}

// 饼图数据类型（产量占比）
interface PieChartData {
  name: string;      // 类别名称（如：车间A、车间B）
  value: number;     // 产量值
}

// 折线图数据类型
interface LineChartData {
  xAxis: string[];   // X轴标签（如：日期、周数）
  series: {
    name: string;    // 系列名称
    data: number[];  // 数值数组
  }[];
}

// 接口请求参数类型
interface ProductionQueryParams {
  factoryId?: string; // 工厂ID
  warehouseId?: string; // 仓库ID
}

// ===================== 状态管理 =====================
// 加载状态
const isLoading = ref<boolean>(false);

// 产量核心数据
const productionData = reactive<ProductionData>({
  yesterday: 0,
  week: 0,
  month: 0,
  year: 0
});

// 图表实例引用
const pieChartRef = ref<HTMLDivElement | null>(null);
const dayLineChartRef = ref<HTMLDivElement | null>(null);
const weekLineChartRef = ref<HTMLDivElement | null>(null);
const monthLineChartRef = ref<HTMLDivElement | null>(null);

// 图表实例（用于销毁和更新）
let pieChartInstance: echarts.ECharts | null = null;
let dayLineChartInstance: echarts.ECharts | null = null;
let weekLineChartInstance: echarts.ECharts | null = null;
let monthLineChartInstance: echarts.ECharts | null = null;

// 图表数据（预留接口赋值）
const pieChartData = ref<PieChartData[]>([]);
const dayLineChartData = ref<LineChartData>({ xAxis: [], series: [] });
const weekLineChartData = ref<LineChartData>({ xAxis: [], series: [] });
const monthLineChartData = ref<LineChartData>({ xAxis: [], series: [] });

// ===================== 接口调用逻辑（预留） =====================
/**
 * 【预留接口】获取产量核心指标数据
 */
const getProductionData = async (params?: ProductionQueryParams): Promise<ProductionData> => {
  // TODO: 替换为真实接口调用
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        yesterday: 12580,
        week: 89650,
        month: 385600,
        year: 4258900
      });
    }, 800);
  });
};

/**
 * 【预留接口】获取饼图数据（产量占比）
 */
const getPieChartData = async (params?: ProductionQueryParams): Promise<PieChartData[]> => {
  // TODO: 替换为真实接口调用
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve([
        { name: '车间A', value: 185000 },
        { name: '车间B', value: 126000 },
        { name: '车间C', value: 74600 },
        { name: '外协加工', value: 45800 }
      ]);
    }, 800);
  });
};

/**
 * 【预留接口】获取日产量折线图数据
 */
const getDayLineChartData = async (params?: ProductionQueryParams): Promise<LineChartData> => {
  // TODO: 替换为真实接口调用
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        xAxis: ['01日', '02日', '03日', '04日', '05日', '06日', '07日'],
        series: [{
          name: '日产量',
          data: [11800, 12580, 13200, 11950, 12800, 12100, 12580]
        }]
      });
    }, 800);
  });
};

/**
 * 【预留接口】获取周产量折线图数据
 */
const getWeekLineChartData = async (params?: ProductionQueryParams): Promise<LineChartData> => {
  // TODO: 替换为真实接口调用
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        xAxis: ['第1周', '第2周', '第3周', '第4周', '第5周'],
        series: [{
          name: '周产量',
          data: [78500, 82600, 85800, 89650, 87200]
        }]
      });
    }, 800);
  });
};

/**
 * 【预留接口】获取月产量折线图数据
 */
const getMonthLineChartData = async (params?: ProductionQueryParams): Promise<LineChartData> => {
  // TODO: 替换为真实接口调用
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve({
        xAxis: ['1月', '2月', '3月', '4月', '5月', '6月'],
        series: [{
          name: '月产量',
          data: [325000, 348000, 362000, 375000, 380000, 385600]
        }]
      });
    }, 800);
  });
};

// ===================== 图表初始化/更新 =====================
/**
 * 初始化饼图
 */
const initPieChart = () => {
  if (!pieChartRef.value) return;
  // 销毁原有实例
  if (pieChartInstance) {
    pieChartInstance.dispose();
  }
  // 创建新实例
  pieChartInstance = echarts.init(pieChartRef.value);
  // 设置配置项
  const option = {
    color: ['#003399', '#00AEEF', '#0066CC', '#66B2FF'], // HEBANG蓝系配色
    tooltip: {
      trigger: 'item',
      formatter: '{a} <br/>{b}: {c} 件 ({d}%)'
    },
    legend: {
      orient: 'horizontal',
      bottom: 0,
      textStyle: { color: '#333' }
    },
    series: [
      {
        name: '产量占比',
        type: 'pie',
        radius: ['40%', '70%'],
        avoidLabelOverlap: false,
        label: {
          show: false,
          position: 'center'
        },
        emphasis: {
          label: {
            show: true,
            fontSize: 16,
            fontWeight: 600
          }
        },
        labelLine: {
          show: false
        },
        data: pieChartData.value
      }
    ]
  };
  pieChartInstance.setOption(option);
};

/**
 * 初始化折线图通用方法
 */
const initLineChart = (
  instanceRef: HTMLDivElement | null, 
  chartInstance: echarts.ECharts | null, 
  chartData: LineChartData,
  yAxisName: string
): echarts.ECharts | null => {
  if (!instanceRef) return null;
  // 销毁原有实例
  if (chartInstance) {
    chartInstance.dispose();
  }
  // 创建新实例
  const newInstance = echarts.init(instanceRef);
  const option = {
    color: ['#003399'], // HEBANG主深蓝色
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' }
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: chartData.xAxis,
      axisLine: { lineStyle: { color: '#e8f4fc' } },
      axisLabel: { color: '#666' }
    },
    yAxis: {
      type: 'value',
      name: yAxisName,
      nameTextStyle: { color: '#003399' },
      axisLine: { lineStyle: { color: '#e8f4fc' } },
      axisLabel: { color: '#666' },
      splitLine: { lineStyle: { color: '#e8f4fc' } }
    },
    series: chartData.series.map(item => ({
      name: item.name,
      type: 'line',
      smooth: true, // 平滑曲线
      data: item.data,
      lineStyle: { width: 2 },
      itemStyle: {
        color: '#00AEEF', // 浅蓝飘带色
        borderColor: '#003399',
        borderWidth: 2
      },
      areaStyle: {
        color: {
                type: 'linear',
                x: 0,
                y: 0,
                x2: 0,
                y2: 1,
                colorStops: [
                    { offset: 0, color: 'rgba(0, 174, 239, 0.2)' },
                    { offset: 1, color: 'rgba(0, 174, 239, 0.05)' }
                ]
                }
      }
    }))
  };
  newInstance.setOption(option);
  return newInstance;
};

/**
 * 初始化所有图表
 */
const initAllCharts = () => {
  initPieChart();
  dayLineChartInstance = initLineChart(dayLineChartRef.value, dayLineChartInstance, dayLineChartData.value, '产量(件)');
  weekLineChartInstance = initLineChart(weekLineChartRef.value, weekLineChartInstance, weekLineChartData.value, '产量(件)');
  monthLineChartInstance = initLineChart(monthLineChartRef.value, monthLineChartInstance, monthLineChartData.value, '产量(件)');
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

// ===================== 业务方法 =====================
/**
 * 获取所有数据（核心指标+图表）
 */
const fetchAllData = async () => {
  try {
    isLoading.value = true;
    // 构造请求参数
    const params: ProductionQueryParams = {
      // factoryId: '1001', // 示例参数
      // warehouseId: '2001'
    };

    // 并行请求所有数据
    const [prodData, pieData, dayLineData, weekLineData, monthLineData] = await Promise.all([
      getProductionData(params),
      getPieChartData(params),
      getDayLineChartData(params),
      getWeekLineChartData(params),
      getMonthLineChartData(params)
    ]);

    // 更新数据
    Object.assign(productionData, prodData);
    pieChartData.value = pieData;
    dayLineChartData.value = dayLineData;
    weekLineChartData.value = weekLineData;
    monthLineChartData.value = monthLineData;

    // 重新渲染图表
    initAllCharts();
    message.success("全部数据刷新成功");
  } catch (error) {
    console.error("获取数据失败：", error);
    message.error("数据加载失败，请稍后重试");
  } finally {
    isLoading.value = false;
  }
};

// ===================== 生命周期 =====================
// 页面初始化
onMounted(async () => {
  // 加载数据
  await fetchAllData();
  // 监听窗口resize
  window.addEventListener('resize', resizeCharts);
});

// 页面销毁
onUnmounted(() => {
  // 销毁图表实例
  pieChartInstance?.dispose();
  dayLineChartInstance?.dispose();
  weekLineChartInstance?.dispose();
  monthLineChartInstance?.dispose();
  // 移除resize监听
  window.removeEventListener('resize', resizeCharts);
});

// 监听图表数据变化，重新渲染
watch([pieChartData, dayLineChartData, weekLineChartData, monthLineChartData], () => {
  initAllCharts();
}, { deep: true });
</script>

<style scoped>
#leaderDashboardPage {
  padding: 20px;
  background-color: #f9fbfd;
  min-height: calc(100vh - 80px);
}

/* 页面标题样式 */
.page-header {
  margin-bottom: 24px;
}

.page-title {
  margin: 0 0 8px 0;
  font-size: 24px;
  font-weight: 600;
  color: #003399; /* HEBANG主深蓝色 */
}

.page-desc {
  margin: 0;
  color: #666;
  font-size: 14px;
}

/* 上部：产量卡片容器 */
.production-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 20px;
  margin-bottom: 24px;
}

/* 产量卡片样式 */
.production-card {
  border: 1px solid #e8f4fc; /* 浅蓝系边框 */
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 51, 153, 0.06); /* 主色浅阴影 */
  transition: all 0.3s ease;
}

.production-card:hover {
  box-shadow: 0 4px 16px rgba(0, 174, 239, 0.12); /* 辅助色阴影 */
  border-color: #00AEEF; /* HEBANG浅蓝飘带色 */
  transform: translateY(-2px);
}

/* 统计项样式 */
.stat-item {
  --ant-statistic-title-color: #333;
  --ant-statistic-content-color: #003399; /* 数值主深蓝色 */
}

.stat-icon {
  color: #00AEEF; /* 图标浅蓝飘带色 */
  font-size: 20px;
}

/* 图表区域通用样式 */
.chart-section {
  margin-bottom: 24px;
  width: 100%;
}

/* 中部：饼图区域 */
.pie-chart-section .chart-card {
  border: 1px solid #e8f4fc;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 51, 153, 0.06);
}

/* 下部：折线图区域 */
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

/* 图表容器 */
.chart-container {
  width: 100%;
  height: 300px; /* 图表固定高度 */
}

/* 刷新按钮样式 */
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
    height: 250px;
  }
}
</style>
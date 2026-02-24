<template>
  <a-tabs 
    type="card" 
    :style="{ 
      maxWidth: '100%', 
      margin: '20px auto',
      padding: '0 10px' 
    }"
    :tab-bar-gutter="getTabGutter()"
  >
    <a-tab-pane key="data-edit" tab="检测数据">
      <div class="table-container">
        <div class="date-selector">
          <el-date-picker
            v-model="selectedDate"
            type="date"
            placeholder="选择查询日期"
            @change="fetchTableData"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            :disabled-date="disabledFutureDate"
            :picker-options="{
              shortcuts: [
                {
                  text: '今天',
                  onClick: () => {
                    selectedDate.value = new Date().toISOString().split('T')[0];
                    fetchTableData();
                  },
                },
              ],
            }"
            :size="getComponentSize()"
            :style="getDatePickerStyle()" 
          />
          <el-button 
            type="primary" 
            @click="fetchTableData"
            :size="getComponentSize()"
          >
            查询
          </el-button>
          <!-- 新增重载按钮：样式与查询一致 -->
          <el-button 
            type="primary" 
            @click="reloadTableData"
            :size="getComponentSize()"
          >
            重载
          </el-button>
        </div>
        <div class="table-scroll-wrapper">
          <el-table
            :data="tableData"
            border
            style="width: 100%; table-layout: fixed"
            :cell-class-name="cellClassName"
            :size="getComponentSize()"
            empty-text="当前日期暂无小时数据"
            :header-cell-style="getHeaderCellStyle()"
            :cell-style="getCellStyle()"
          >
            <el-table-column
              v-for="(header, index) in tableHeaders"
              :key="index"
              :prop="header.prop"
              :label="header.label"
              :width="getColumnWidth(header.prop)"
              align="center"
              :show-overflow-tooltip="true"
            >
              <template #default="scope">
                <template v-if="header.prop === 'hour'">
                  {{ scope.row[header.prop] }}
                </template>
                <template v-else>
                  <template v-if="isCellDisabled(scope.row)">
                    {{ scope.row[header.prop] || "-" }}
                  </template>
                  <template v-else>
                    <el-input
                      v-model="scope.row[header.prop]"
                      :size="getComponentSize()"
                      @blur="handleCellEdit(scope.row, header.prop)"
                      :disabled="isCellDisabled(scope.row)"
                      maxlength="8"
                      :style="{ width: getInputWidth() }"
                    />
                  </template>
                </template>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </a-tab-pane>
    <a-tab-pane key="data-view" tab="数据预览">
      <div style="padding: 20px; text-align: center" :style="{ fontSize: getFontSize() }">
        数据预览模块（可自定义内容）
      </div>
    </a-tab-pane>
  </a-tabs>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from "vue";
import { ElMessage } from "element-plus";
// 导入核心接口，ReloadData为重载接口（已取消注释，直接调用）
import { Headers, HourData, SaveCell, ReloadData } from "@/api/TableEdit";

// 表格表头类型：prop匹配后端cells的key（cell29/cell30等）
interface TableHeader {
  prop: string;
  label: string;
}

// 后端返回的小时数据项结构（精准适配，全小写）
interface HourDataItem {
  hour: number;
  date: string;
  isNextDay: boolean; // 后端返回的禁用标识，true=禁用/false=可编辑
  cells?: Record<string, string>;
}

// 前端表格行类型：继承后端结构+动态字段兼容
interface TableRow extends HourDataItem {
  [key: string]: any;
}

// 【重载接口参数类型】严格匹配后端要求：type固定1，time为日期字符串
interface ReloadDataParams {
  type: number;
  time: string;
}

// 响应式数据
const selectedDate = ref<string>(""); // 选中日期
const tableHeaders = ref<TableHeader[]>([]); // 表格表头
const tableData = ref<TableRow[]>([]); // 表格数据
const screenWidth = ref<number>(window.innerWidth); // 屏幕宽度

// 监听窗口大小变化
const handleResize = () => {
  screenWidth.value = window.innerWidth;
};

// 计算屏幕类型（仅电脑端分级）
const screenGrade = computed(() => {
  if (screenWidth.value < 1366) return "small"; // 小屏笔记本（1366*768）
  if (screenWidth.value < 1920) return "normal"; // 常规屏（1920*1080）
  return "large"; // 大屏显示器（2K/4K）
});

// 获取组件尺寸（按钮/输入框/表格）
const getComponentSize = () => {
  return screenGrade.value === "small" ? "small" : "default";
};

// 获取标签栏间距
const getTabGutter = () => {
  return screenGrade.value === "small" ? 8 : 16;
};

// 核心调整：控制日期选择器宽度（解决过宽问题）
const getDatePickerStyle = () => {
  const styles: Record<string, string> = {
    flexShrink: "0", // 取消flex:1，避免占满剩余宽度
    padding: "0 4px"
  };
  // 不同屏幕尺寸设置固定宽度上限
  switch (screenGrade.value) {
    case "small":
      styles.width = "180px"; // 小屏：窄一点
      break;
    case "normal":
      styles.width = "200px"; // 常规屏：适中
      break;
    case "large":
      styles.width = "220px"; // 大屏：略宽但不夸张
      break;
  }
  return styles;
};

// 获取列宽度
const getColumnWidth = (prop: string) => {
  if (prop === "hour") {
    return screenGrade.value === "small" ? 50 : 60;
  }
  return screenGrade.value === "small" ? 80 : screenGrade.value === "large" ? 100 : 90;
};

// 获取输入框宽度
const getInputWidth = () => {
  return screenGrade.value === "small" ? "70px" : screenGrade.value === "large" ? "90px" : "80px";
};

// 获取表头样式
const getHeaderCellStyle = () => {
  const fontSize = screenGrade.value === "small" ? "11px" : screenGrade.value === "large" ? "13px" : "12px";
  return {
    fontSize,
    padding: "2px 0",
  };
};

// 获取单元格样式
const getCellStyle = () => {
  const fontSize = screenGrade.value === "small" ? "10px" : screenGrade.value === "large" ? "14px" : "13px";
  return {
    fontSize,
    padding: "2px 0",
  };
};

// 获取字体大小（预览区域）
const getFontSize = () => {
  return screenGrade.value === "small" ? "12px" : screenGrade.value === "large" ? "14px" : "13px";
};

// 禁用未来日期选择（日期选择器的禁用，与表格单元格禁用无关）
const disabledFutureDate = (date: Date): boolean => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const selectDate = new Date(date);
  selectDate.setHours(0, 0, 0, 0);
  return selectDate.getTime() > today.getTime();
};

// 判断单元格是否禁用：核心逻辑→根据后端返回的isNextDay字段，true禁用/false可编辑
const isCellDisabled = (row: TableRow): boolean => {
  // 基础非空校验：行数据的日期/小时缺失时，默认禁用单元格
  if (!row.date || row.hour === undefined || row.hour === null) return true;
  // 核心规则：完全遵循后端返回的isNextDay标识
  return row.isNextDay === true;
};

// 单元格样式：禁用/小时列添加灰色背景（基于isCellDisabled判断）
const cellClassName = ({
  row,
  column,
}: {
  row: TableRow;
  column: any;
}): string => {
  if (column.prop === "hour") return "disabled-cell";
  return isCellDisabled(row) ? "disabled-cell" : "";
};

// 获取表格表头（从后端接口拉取，强制hour列排第一）
const fetchTableHeaders = async (): Promise<void> => {
  try {
    const res = await Headers();
    if (res?.data) {
      tableHeaders.value = res.data;
      const hourHeader = tableHeaders.value.find(
        (item) => item.prop === "hour"
      );
      if (hourHeader) {
        tableHeaders.value = [
          hourHeader,
          ...tableHeaders.value.filter((item) => item.prop !== "hour"),
        ];
      }
    }
  } catch (error) {
    ElMessage.error("获取表格表头失败，请刷新页面");
    console.error("fetchTableHeaders error:", error);
  }
};

// 获取指定日期的小时数据，解析后端cells字段为表格可渲染的动态字段
const fetchTableData = async (): Promise<void> => {
  if (!selectedDate.value) {
    ElMessage.warning("请先选择查询日期");
    return;
  }

  try {
    const res = await HourData({ date: selectedDate.value });
    const originData = res?.data || [];

    if (originData.length === 0) {
      tableData.value = [];
      ElMessage.info(`【${selectedDate.value}】暂无小时数据`);
      return;
    }

    const formatTableData = originData.map((item: HourDataItem) => {
      if (!item)
        return {
          hour: 0,
          date: selectedDate.value,
          isNextDay: false,
          cells: {},
        } as TableRow;
      const cellData = item.cells || {};
      return { ...item, ...cellData } as TableRow;
    });

    tableData.value = formatTableData;
    ElMessage.success(`【${selectedDate.value}】小时数据加载成功`);
  } catch (error) {
    ElMessage.error("小时数据加载失败，请重试");
    console.error("fetchTableData error:", error);
  }
};

// 单元格编辑失焦保存，仅保存非禁用、非小时列的修改
const handleCellEdit = async (row: TableRow, prop: string): Promise<void> => {
  if (prop === "hour" || isCellDisabled(row)) return;

  const saveParams = {
    date: row.date,
    hour: row.hour,
    prop: prop,
    value: row[prop] || "",
  };

  try {
    await SaveCell(saveParams);
    ElMessage.success(`已保存：${row.date} ${row.hour}点 - ${prop} 字段`);
  } catch (error) {
    ElMessage.error("单元格数据保存失败，请重试");
    console.error("handleCellEdit error:", error);
  }
};

// 重载表格数据：调用后端重载接口（参数type=1，time=选中日期），重载后刷新表格
const reloadTableData = async (): Promise<void> => {
  if (!selectedDate.value) {
    ElMessage.warning("请先选择查询日期");
    return;
  }

  try {
    ElMessage.info(`正在重载【${selectedDate.value}】数据，请稍候...`);
    //选中日期加1天(重建excle报表时需要传明天的日期)
    const nextDay = new Date(selectedDate.value); // 构造日期对象
    nextDay.setDate(nextDay.getDate() + 1); // 日期加1天（自动处理月底/跨年）
    // 构造重载参数：严格匹配后端要求，type固定传1
    const reloadParams: ReloadDataParams = {
      type: 1,
      time: nextDay.toISOString().split("T")[0],
    };
    // 调用后端重载接口
    await ReloadData(reloadParams);
    // 重载成功后重新拉取数据，保证表格数据最新（含最新的isNextDay标识）
    await fetchTableData();
    ElMessage.success(`【${selectedDate.value}】数据重载完成`);
  } catch (error) {
    ElMessage.error("数据重载失败，请重试");
    console.error("reloadTableData error:", error);
  }
};

// 页面挂载初始化：先加载表头，再加载今日数据
onMounted(async () => {
  // 监听窗口大小变化
  window.addEventListener("resize", handleResize);
  await fetchTableHeaders();
  selectedDate.value = new Date().toISOString().split("T")[0];
  await fetchTableData();
});

// 组件卸载时移除监听
onUnmounted(() => {
  window.removeEventListener("resize", handleResize);
});
</script>

<style scoped>
:deep(.ant-tabs-card) {
  --ant-tabs-card-head-background: #f8f9fa;
  border-radius: 4px;
  width: 100%;
}

/* 日期选择器+按钮布局，新增重载按钮后间距保持一致 */
.date-selector {
  margin-bottom: 10px;
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
  padding: 0 4px;
  width: 100%;
}

/* 核心调整：限制日期选择器容器的最大宽度，避免挤压按钮 */
:deep(.el-date-picker) {
  max-width: 220px !important;
  width: 100% !important;
}

/* 表格滚动容器 - 核心自适应样式 */
.table-scroll-wrapper {
  /* 根据屏幕尺寸动态调整高度偏移 */
  height: calc(100vh - var(--table-offset));
  width: 100%;
  overflow: auto;
  box-sizing: border-box;
  padding: 0 1px;
  margin: 4px 0;
}

/* 不同电脑屏幕尺寸的表格高度偏移 */
@media screen and (max-width: 1366px) {
  .table-scroll-wrapper {
    --table-offset: 110px;
  }
  .date-selector {
    gap: 8px;
  }
  /* 小屏额外缩小日期选择器 */
  :deep(.el-date-picker) {
    max-width: 180px !important;
  }
}

@media screen and (min-width: 1367px) and (max-width: 1919px) {
  .table-scroll-wrapper {
    --table-offset: 120px;
  }
}

@media screen and (min-width: 1920px) {
  .table-scroll-wrapper {
    --table-offset: 130px;
  }
}

/* 滚动条样式自适应 */
.table-scroll-wrapper::-webkit-scrollbar {
  height: 12px;
  width: 8px;
}

.table-scroll-wrapper::-webkit-scrollbar-thumb {
  background-color: #ccc;
  border-radius: 3px;
}

/* 表格单元格紧凑样式 */
:deep(.el-table td),
:deep(.el-table th) {
  padding: 2px 0 !important;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* 禁用单元格样式 */
.disabled-cell {
  background-color: #f5f5f5;
  color: #999;
  cursor: not-allowed;
}

/* 输入框样式适配 */
:deep(.el-input__wrapper) {
  padding: 0 5px !important;
  box-sizing: border-box;
}

/* 日期选择器禁用样式优化 */
:deep(.el-picker-panel__content .el-date-table td.disabled) {
  color: #ccc !important;
  cursor: not-allowed !important;
}

/* 适配小屏电脑的表格内容 */
@media screen and (max-width: 1366px) {
  :deep(.el-table th .cell) {
    font-weight: 500;
  }
  :deep(.el-input__wrapper) {
    font-size: 12px;
  }
}

/* 适配大屏电脑的表格内容 */
@media screen and (min-width: 1920px) {
  :deep(.el-table th .cell) {
    font-size: 14px;
    font-weight: 600;
  }
  :deep(.el-table td .cell) {
    font-size: 14px;
  }
  :deep(.el-input__wrapper) {
    font-size: 14px;
  }
}
</style>
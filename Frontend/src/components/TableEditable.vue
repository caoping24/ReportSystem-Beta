<template>
  <!-- Ant Design Vue 标签页容器 -->
  <a-tabs type="card" style="max-width: 1800px; margin: 20px auto;">
    <!-- 数据编辑标签面板 -->
    <a-tab-pane key="data-edit" tab="小时数据编辑">
      <div class="table-container">
        <!-- 日期选择器：保留原有功能 -->
        <div class="date-selector">
          <el-date-picker
            v-model="selectedDate"
            type="date"
            placeholder="选择查询日期"
            @change="fetchTableData"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            :disabled-date="disabledFutureDate"
            :picker-options="{ shortcuts: [{ text: '今天', onClick: () => { selectedDate.value = new Date().toISOString().split('T')[0]; fetchTableData(); } }] }"
          />
          <el-button type="primary" @click="fetchTableData">查询</el-button>
        </div>

        <!-- 可编辑表格：调整列宽适配更大字体 -->
        <div class="table-scroll-wrapper">
          <el-table
            :data="tableData"
            border
            style="width: 100%; table-layout: fixed;"
            :cell-class-name="cellClassName"
            size="small"
          >
            <!-- 动态表头：微调列宽适配更大字体 -->
            <el-table-column
              v-for="(header, index) in tableHeaders"
              :key="index"
              :prop="header.prop"
              :label="header.label"
              :width="header.prop === 'hour' ? 60 : 90"
              align="center"
            >
              <template #default="scope">
                <!-- 小时字段仅展示文本 -->
                <template v-if="header.prop === 'hour'">
                  {{ scope.row[header.prop] }}
                </template>
                <!-- 其他字段判断是否可编辑 -->
                <template v-else>
                  <template v-if="isCellDisabled(scope.row)">
                    {{ scope.row[header.prop] || '-' }}
                  </template>
                  <template v-else>
                    <el-input
                      v-model="scope.row[header.prop]"
                      size="mini"
                      @blur="handleCellEdit(scope.row, header.prop)"
                      :disabled="isCellDisabled(scope.row)"
                      maxlength="6"
                      style="width: 80px;" 
                    />
                  </template>
                </template>
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </a-tab-pane>

    <!-- 可新增其他标签页（示例） -->
    <a-tab-pane key="data-view" tab="数据预览">
      <div style="padding: 20px; text-align: center;">
        数据预览模块（可自定义内容）
      </div>
    </a-tab-pane>
  </a-tabs>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';

// 定义类型
interface TableHeader {
  prop: string; // 字段名
  label: string; // 表头显示文本
}

interface TableRow {
  hour: number; // 小时（8-24, 0-7）
  date: string; // 数据日期
  [key: string]: any; // 动态字段
}

// 响应式数据
const selectedDate = ref<string>(''); // 选择的查询日期
const tableHeaders = ref<TableHeader[]>([]); // 表头数据
const tableData = ref<TableRow[]>([]); // 表格数据

// 禁用未来日期的方法
const disabledFutureDate = (date: Date): boolean => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const todayTime = today.getTime();
  
  const selectDate = new Date(date);
  selectDate.setHours(0, 0, 0, 0);
  const selectTime = selectDate.getTime();
  
  return selectTime > todayTime;
};

// 判断单元格是否禁用
const isCellDisabled = (row: TableRow): boolean => {
  if (!row.date) return false;

  const currentDateStr = new Date().toISOString().split('T')[0];
  const currentTime = new Date().getTime();

  const rowTimeStr = `${row.date} ${row.hour.toString().padStart(2, '0')}:00:00`;
  const rowTime = new Date(rowTimeStr).getTime();

  const isFutureTime = rowTime > currentTime;
  const isTodayNextDayHours = row.date === currentDateStr && row.hour >= 0 && row.hour <= 7;

  return isFutureTime || isTodayNextDayHours;
};

// 单元格样式（禁用标灰）
const cellClassName = ({ row, column }: { row: TableRow; column: any }): string => {
  if (column.prop === 'hour') return 'disabled-cell';
  return isCellDisabled(row) ? 'disabled-cell' : '';
};

// 模拟获取30个字段的表头
const fetchTableHeaders = async (): Promise<TableHeader[]> => {
  const baseHeaders = [{ prop: 'hour', label: '小时' }];
  const dynamicHeaders = [
    '温度', '湿度', '气压', '风速', '风向',
    '能见度', '降水量', '辐射', '露点', '云量',
    '雾浓度', '霾浓度', '紫外线', 'PM2.5', 'PM10',
    'CO', 'SO2', 'NO2', 'O3', '噪音',
    '蒸发量', '日照', '雪深', '冰厚', '涡度',
    '散度', '垂直速度', '水汽通量', '边界层'
  ].map((label, index) => ({
    prop: `field_${index + 1}`,
    label
  }));

  return [...baseHeaders, ...dynamicHeaders];
};

// 模拟获取表格数据
const fetchTableData = async (): Promise<void> => {
  if (!selectedDate.value) {
    ElMessage.warning('请选择查询日期');
    return;
  }

  try {
    const hourList = [8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,0,1,2,3,4,5,6,7];
    const mockData: TableRow[] = hourList.map(hour => {
      const row: TableRow = { hour, date: selectedDate.value };
      for (let i = 1; i <= 29; i++) {
        row[`field_${i}`] = (Math.random() * 100).toFixed(1);
      }
      return row;
    });

    tableData.value = mockData;
    ElMessage.success('数据加载成功');
  } catch (error) {
    ElMessage.error('数据加载失败');
    console.error('fetchTableData error:', error);
  }
};

// 单元格编辑回调
const handleCellEdit = async (row: TableRow, prop: string): Promise<void> => {
  if (prop === 'hour' || isCellDisabled(row)) return;

  try {
    ElMessage.success(`已修改 ${row.date} ${row.hour}点 的${prop}字段为：${row[prop]}`);
  } catch (error) {
    ElMessage.error('修改失败，请重试');
    console.error('handleCellEdit error:', error);
  }
};

// 初始化
onMounted(async () => {
  const headers = await fetchTableHeaders();
  tableHeaders.value = headers;
  const today = new Date().toISOString().split('T')[0];
  selectedDate.value = today;
  await fetchTableData();
});
</script>

<style scoped>
/* 标签页容器样式：限制宽度，避免占满页面 */
:deep(.ant-tabs-card) {
  --ant-tabs-card-head-background: #f8f9fa;
  border-radius: 4px;
}

/* 表格外层容器 */
.table-container {
  padding: 15px;
}

/* 日期选择器容器 */
.date-selector {
  margin-bottom: 10px;
  display: flex;
  gap: 10px;
  align-items: center;
}

/* 表格横向滚动容器：适配30个窄列 */
.table-scroll-wrapper {
  width: 100%;
  overflow-x: auto;
  scrollbar-width: thin;
  scrollbar-color: #ccc #f5f5f5;
}

/* 优化webkit滚动条 */
.table-scroll-wrapper::-webkit-scrollbar {
  height: 6px;
}
.table-scroll-wrapper::-webkit-scrollbar-thumb {
  background-color: #ccc;
  border-radius: 3px;
}

/* 调整单元格内边距，适配更大字体 */
:deep(.el-table td),
:deep(.el-table th) {
  padding: 4px 0 !important; /* 恢复到4px内边距 */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* 调大字体：适配窄列且清晰可读 */
:deep(.el-table th .cell) {
  font-size: 14px; /* 表头字体：从11px → 14px */
  font-weight: 500;
}
:deep(.el-table td .cell) {
  font-size: 13px; /* 单元格字体：从10px → 13px */
}

/* 禁用单元格样式 */
.disabled-cell {
  background-color: #f5f5f5;
  color: #999;
  cursor: not-allowed;
}

/* 输入框适配：调大字体 + 适配列宽 */
:deep(.el-input--mini) {
  width: 80px !important;
}
:deep(.el-input__wrapper) {
  padding: 0 5px !important;
  font-size: 13px; /* 输入框字体同步调大 */
}

/* 禁用日期样式 */
:deep(.el-picker-panel__content .el-date-table td.disabled) {
  color: #ccc !important;
  cursor: not-allowed !important;
}
</style>
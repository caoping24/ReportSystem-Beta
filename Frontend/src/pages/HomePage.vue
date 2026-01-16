<template>
  <div id="userManagePage">
    <a-tabs 
      default-active-key="1" 
      type="card"
      @change="handleTabChange"
    >
      <!-- 原有4个报表标签页（简化版） -->
      <a-tab-pane v-for="item in reportTabs" :key="item.key" :tab="item.tab">
        <report-table
          :tab-key="item.key"
          :columns="columns"
          :data-source="tableData[item.key]?.list || []"
          :pagination-config="paginationConfig"
          :pagination-params="paginationParams"
          @download="downloadExcel"
        />
      </a-tab-pane>

      <!-- 批量下载报表标签页 -->
      <a-tab-pane tab="批量下载报表" key="5">
        <div class="batch-download-container">
          <!-- 标题和说明 -->
          <div class="batch-download-header">
            <h3 class="batch-download-title">批量报表下载</h3>
            <p class="batch-download-desc">
              根据选择的报表类型和时间范围，批量下载报表文件并打包为ZIP格式
            </p>
          </div>
          
          <!-- 修改点1：添加ConfigProvider包裹表单，配置中文语言 -->
          <ConfigProvider :locale="zhCN">
            <!-- 表单区域 -->
            <a-form 
              class="batch-download-form"
              layout="vertical" 
              :label-col="{ span: 4 }" 
              :wrapper-col="{ span: 20 }"
            >
              <a-form-item 
                label="报表类型" 
                :validate-status="!batchReportType ? 'error' : ''"
                :help="!batchReportType ? '请选择报表类型' : ''"
              >
                <a-select 
                  v-model:value="batchReportType" 
                  style="width: 100%; max-width: 300px;"
                  placeholder="请选择报表类型"
                  allow-clear
                  @change="handleReportTypeChange"
                >
                  <a-select-option 
                    v-for="item in reportTabs.filter(item => item.key !== '4')" 
                    :key="item.key" 
                    :value="item.key"
                  >
                    {{ item.tab }}
                  </a-select-option>
                </a-select>
              </a-form-item>

              <div class="date-range-group">
                <a-form-item 
                  label="开始时间" 
                  :validate-status="!batchStartDate && batchReportType ? 'error' : ''"
                  :help="!batchStartDate && batchReportType ? '请选择开始时间' : ''"
                >
                  <a-date-picker
                    v-model:value="batchStartDate"
                    :picker="batchDatePickerType"
                    style="width: 100%; max-width: 300px;"
                    placeholder="选择开始时间"
                    :format="batchDateFormat"
                    allow-clear
                    :disabled-date="disabledFutureDate"
                  />
                </a-form-item>

                <a-form-item 
                  label="结束时间" 
                  :validate-status="!batchEndDate && batchReportType ? 'error' : ''"
                  :help="!batchEndDate && batchReportType ? '请选择结束时间' : ''"
                >
                  <a-date-picker
                    v-model:value="batchEndDate"
                    :picker="batchDatePickerType"
                    style="width: 100%; max-width: 300px;"
                    placeholder="选择结束时间"
                    :format="batchDateFormat"
                    allow-clear
                    :disabled-date="disabledFutureDate"
                  />
                </a-form-item>
              </div>

              <!-- 操作按钮区域 -->
              <a-form-item :wrapper-col="{ offset: 4 }">
                <a-button 
                  type="primary" 
                  @click="batchDownloadZip"
                  :loading="isBatchDownloading"
                  class="batch-download-btn"
                >
                  <template #icon><DownloadOutlined /></template>
                  批量下载ZIP
                </a-button>
                
                <a-button 
                  @click="resetBatchForm"
                  style="margin-left: 12px;"
                >
                  重置
                </a-button>
                
                <div class="batch-tips">
                  <InfoCircleOutlined style="color: #1890ff; margin-right: 4px;" />
                  提示：下载的ZIP包包含所选时间范围内的所有对应类型报表文件
                </div>
              </a-form-item>
            </a-form>
          </ConfigProvider>
        </div>
      </a-tab-pane>
    </a-tabs>
  </div>
</template>

<script lang="ts" setup>
import { getReportByPage, downloadReport, batchDownloadReportZip } from "@/api/user";
import { message } from "ant-design-vue";
import { ref, reactive, computed } from "vue";
import dayjs from "dayjs"; 
import { DownloadOutlined, InfoCircleOutlined } from '@ant-design/icons-vue';
import ReportTable from '@/components/ReportTable.vue';

// 修改点2：导入Antd国际化配置和中文语言包
import { ConfigProvider } from 'ant-design-vue';
import zhCN from 'ant-design-vue/es/locale/zh_CN';
// 导入dayjs中文语言包并配置
import 'dayjs/locale/zh-cn';
dayjs.locale('zh-cn'); // 全局设置dayjs为中文

// ===================== 类型定义（精简整合） =====================
interface ReportItem {
  id: string;
  createdtime: string | Date;
}

interface TableDataItem {
  list: ReportItem[];
}

interface ReportTabItem {
  key: string;
  tab: string;
}

// ===================== 常量定义 =====================
// 报表标签配置（保留原有配置，仅在批量下载处过滤年报表）
const reportTabs: ReportTabItem[] = [
  { key: '1', tab: '日报表' },
  { key: '2', tab: '周报表' },
  { key: '3', tab: '月报表' },
  { key: '4', tab: '年报表' },
];

// 表格列配置（精简）
const columns = [
  { title: "序号", key: "index", width: 80, align: "center" },
  { title: "创建时间", dataIndex: "createTime", key: "createTime" },
  { title: "操作", key: "action", width: 120, align: "center" },
];

// ===================== 状态管理 =====================
const activeTabKey = ref("1");
const isFetching = ref(false);

// 分页参数
const paginationParams = reactive({
  pageIndex: 1,
  pageSize: 10,
  total: 0
});

// 表格数据
const tableData: Record<string, TableDataItem> = reactive({
  "1": { list: [] },
  "2": { list: [] },
  "3": { list: [] },
  "4": { list: [] }
});

// 批量下载相关状态
const batchReportType = ref<string>("");
const batchStartDate = ref<dayjs.Dayjs | null>(null);
const batchEndDate = ref<dayjs.Dayjs | null>(null);
const isBatchDownloading = ref<boolean>(false);

// ===================== 计算属性（简化年报表相关逻辑） =====================
// 修改点3：移除年报表（key=4）的映射，仅保留月报（key=3）
const typeMap = { 
  '3': 'month' 
} as Record<string, 'date' | 'month'>;

// 修改点4：将日期格式改为中文显示（YYYY年MM月 / YYYY年MM月DD日）
const formatMap = { 
  '3': 'YYYY年MM月' 
} as Record<string, string>;

// 分页配置
const paginationConfig = computed(() => ({
  current: paginationParams.pageIndex,
  pageSize: paginationParams.pageSize,
  total: paginationParams.total,
  pageSizeOptions: ["10", "20", "50", "100"],
  showSizeChanger: true,
  showQuickJumper: true,
  showTotal: (total: number) => `共 ${total} 条记录`,
  onChange: (page: number, pageSize: number) => {
    paginationParams.pageIndex = page;
    paginationParams.pageSize = pageSize;
    reportTabs.map(item => item.key).includes(activeTabKey.value) && fetchData(activeTabKey.value);
  },
  onShowSizeChange: (current: number, size: number) => {
    paginationParams.pageIndex = 1;
    paginationParams.pageSize = size;
    reportTabs.map(item => item.key).includes(activeTabKey.value) && fetchData(activeTabKey.value);
  }
}));

// 修改点5：简化批量下载日期选择器类型（仅处理日报/周报=date，月报=month）
const batchDatePickerType = computed(() => {
  return batchReportType.value ? typeMap[batchReportType.value] || "date" : "date";
});

// 修改点6：简化批量下载日期格式化（改为中文格式：YYYY年MM月DD日 / YYYY年MM月）
const batchDateFormat = computed(() => {
  return batchReportType.value ? formatMap[batchReportType.value] || "YYYY年MM月DD日" : "YYYY年MM月DD日";
});

// ===================== 方法定义（封装精简） =====================
// 禁用未来日期
const disabledFutureDate = (current: dayjs.Dayjs) => {
  return current?.isAfter(dayjs().endOf('day')) || false;
};

// 校验批量下载参数
const validateBatchParams = () => {
  if (!batchReportType.value) {
    message.warning("请选择报表类型");
    return false;
  }
  if (!batchStartDate.value || !batchEndDate.value) {
    message.warning("请选择开始时间和结束时间");
    return false;
  }
  if (batchStartDate.value.isAfter(batchEndDate.value)) {
    message.warning("开始时间不能晚于结束时间");
    return false;
  }
  return true;
};

// 处理文件下载（通用下载逻辑封装）
const handleFileDownload = (res: any, defaultFileName: string, fileType: 'xlsx' | 'zip') => {
  const typeMap = {
    xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    zip: 'application/zip'
  };
  
  const blob = new Blob([res.data], { type: typeMap[fileType] });
  const contentDisposition = res.headers?.['content-disposition'];
  let fileName = defaultFileName;

  // 解析文件名
  if (contentDisposition) {
    const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (utf8Match?.[1]) {
      fileName = decodeURIComponent(utf8Match[1]);
    } else {
      const normalMatch = contentDisposition.match(/filename=([^;]+)/i);
      if (normalMatch?.[1]) {
        fileName = decodeURIComponent(normalMatch[1].replace(/['"]/g, ''));
      }
    }
  }

  // 触发下载
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  
  // 清理资源
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
  
  message.success(`${fileType === 'xlsx' ? '报表' : 'ZIP包'}下载成功`);
};

// 单个报表下载
const downloadExcel = async (tabKey: string, id: string) => {
  if (!id) return message.warning("ID 不能为空");
  
  try {
    const res = await downloadReport(Number(id), Number(tabKey));
    handleFileDownload(res, `报表.xlsx`, 'xlsx');
  } catch (error) {
    console.error("报表下载失败：", error);
    message.error("下载失败：网络异常或接口错误");
  }
};

// 批量下载ZIP
const batchDownloadZip = async () => {
  if (!validateBatchParams()) return;

  const params = {
    type: Number(batchReportType.value),
    startTime: batchStartDate.value!.format(batchDateFormat.value),
    endTime: batchEndDate.value!.format(batchDateFormat.value)
  };

  try {
    isBatchDownloading.value = true;
    const res = await batchDownloadReportZip(params);
    handleFileDownload(res, `批量报表_${dayjs().format("YYYYMMDDHHmmss")}.zip`, 'zip');
  } catch (error) {
    console.error("批量下载ZIP失败：", error);
    message.error("批量下载失败：网络异常或接口错误");
  } finally {
    isBatchDownloading.value = false;
  }
};

// 标签切换处理
const handleTabChange = (key: string) => {
  activeTabKey.value = key;
  if (key !== "5") {
    paginationParams.pageIndex = 1;
    fetchData(key);
  }
};

// 报表类型变更处理
const handleReportTypeChange = () => {
  batchStartDate.value = null;
  batchEndDate.value = null;
};

// 重置批量下载表单
const resetBatchForm = () => {
  batchReportType.value = "";
  batchStartDate.value = null;
  batchEndDate.value = null;
};

// 获取报表数据
const fetchData = async (tabKey: string) => {
  if (isFetching.value) return;
  
  try {
    isFetching.value = true;
    const res = await getReportByPage({
      pageIndex: paginationParams.pageIndex,
      pageSize: paginationParams.pageSize,
      type: Number(tabKey)
    });
    
    if (res.data) {
      tableData[tabKey].list = res.data.data || [];
      paginationParams.total = res.data.totalCount || 0;
    } else {
      message.error(`获取${tabKey}号表格数据失败`);
    }
  } catch (error) {
    console.error(`获取${tabKey}号表格数据异常：`, error);
    message.error(`获取${tabKey}号表格数据失败：网络异常`);
  } finally {
    isFetching.value = false;
  }
};

// 初始化加载数据
fetchData("1");
</script>

<style scoped>
#userManagePage {
  padding: 20px;
}

.ant-tabs-card > .ant-tabs-nav .ant-tabs-tab {
  padding: 12px 24px;
}

.ant-table {
  margin-top: 16px;
}

/* 批量下载容器样式（精简） */
.batch-download-container {
  margin-top: 16px;
  padding: 24px;
  background: #ffffff;
  border-radius: 12px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.06);
  border: 1px solid #f0f0f0;
}

.batch-download-header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid #f0f0f0;
}

.batch-download-title {
  margin: 0 0 8px 0;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.batch-download-desc {
  margin: 0;
  color: #666666;
  font-size: 14px;
  line-height: 1.5;
}

.batch-download-form {
  max-width: 800px;
}

.date-range-group {
  display: flex;
  gap: 24px;
  margin-bottom: 8px;
  flex-wrap: wrap;
}

.date-range-group .ant-form-item {
  flex: 1;
  min-width: 280px;
  margin-bottom: 16px !important;
}

.batch-download-btn {
  height: 40px;
  padding: 0 24px;
}

.batch-tips {
  margin-top: 12px;
  font-size: 12px;
  color: #666666;
  display: flex;
  align-items: center;
}

/* 响应式适配 */
@media (max-width: 768px) {
  .batch-download-container {
    padding: 16px;
  }
  
  .date-range-group {
    flex-direction: column;
    gap: 0;
  }
  
  .date-range-group .ant-form-item {
    min-width: 100%;
  }
}
</style>
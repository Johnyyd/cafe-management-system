import React, { useState, useEffect } from 'react';
import {
  Activity,
  Server,
  Clock,
  Database,
  RefreshCw,
  Cpu,
  CheckCircle2,
  Upload,
  AlertTriangle,
  HardDriveDownload,
  FileJson,
} from 'lucide-react';
import { PageContainer } from '../components/layout/PageContainer';
import { Card } from '../components/common/Card';
import { Button } from '../components/common/Button';
import { Badge } from '../components/common/Badge';
import { Modal } from '../components/common/Modal';
import { useAppStore } from '../stores/appStore';
import { metricsApi, backupApi } from '../api/client';
import { SystemMetrics } from '../types';

export const MetricsPage: React.FC = () => {
  const [metrics, setMetrics] = useState<SystemMetrics | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const refreshInterval = 5000;
  const addNotification = useAppStore((state) => state.addNotification);

  // Backup & Restore state
  const [backupSummary, setBackupSummary] = useState<{
    databaseName: string;
    totalCollections: number;
    collections: Record<string, number>;
    timestamp: string;
  } | null>(null);
  const [backupLoading, setBackupLoading] = useState<boolean>(false);
  const [isRestoreModalOpen, setIsRestoreModalOpen] = useState<boolean>(false);
  const [restoreJsonContent, setRestoreJsonContent] = useState<any | null>(null);
  const [restoreFileName, setRestoreFileName] = useState<string>('');
  const [restoreLoading, setRestoreLoading] = useState<boolean>(false);

  const fetchMetrics = async () => {
    try {
      const data = await metricsApi.getSystemMetrics();
      setMetrics(data);
    } catch {
      addNotification({
        type: 'error',
        title: 'Lỗi tải chỉ số',
        message: 'Không thể kết nối đến điểm đo kiểm tra hiệu năng hệ thống.',
      });
    } finally {
      setLoading(false);
    }
  };

  const fetchBackupSummary = async () => {
    try {
      setBackupLoading(true);
      const data = await backupApi.getSummary();
      setBackupSummary(data);
    } catch {
      // ignore
    } finally {
      setBackupLoading(false);
    }
  };

  useEffect(() => {
    fetchMetrics();
    fetchBackupSummary();
    const interval = setInterval(fetchMetrics, refreshInterval);
    return () => clearInterval(interval);
  }, [refreshInterval]);

  const handleExportBackup = async () => {
    try {
      setBackupLoading(true);
      const data = await backupApi.exportData();
      const jsonString = `data:text/json;charset=utf-8,${encodeURIComponent(JSON.stringify(data, null, 2))}`;
      const downloadAnchor = document.createElement('a');
      const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
      downloadAnchor.setAttribute('href', jsonString);
      downloadAnchor.setAttribute('download', `cafe_management_backup_${timestamp}.json`);
      document.body.appendChild(downloadAnchor);
      downloadAnchor.click();
      downloadAnchor.remove();

      addNotification({
        type: 'success',
        title: 'Xuất sao lưu thành công',
        message: 'File sao lưu toàn bộ cơ sở dữ liệu đã được tải về máy của bạn.',
      });
    } catch (err: any) {
      addNotification({
        type: 'error',
        title: 'Lỗi sao lưu',
        message: err.message || 'Không thể xuất dữ liệu sao lưu.',
      });
    } finally {
      setBackupLoading(false);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setRestoreFileName(file.name);
    const reader = new FileReader();
    reader.onload = (event) => {
      try {
        const parsed = JSON.parse(event.target?.result as string);
        if (!parsed.collections || typeof parsed.collections !== 'object') {
          throw new Error('File sao lưu không đúng định dạng chuẩn của Cafe Management System');
        }
        setRestoreJsonContent(parsed);
      } catch (err: any) {
        addNotification({
          type: 'error',
          title: 'File không hợp lệ',
          message: err.message || 'Không thể đọc nội dung file JSON.',
        });
        setRestoreJsonContent(null);
      }
    };
    reader.readAsText(file);
  };

  const handleExecuteRestore = async () => {
    if (!restoreJsonContent) return;
    try {
      setRestoreLoading(true);
      const result = await backupApi.restoreData(restoreJsonContent);
      addNotification({
        type: 'success',
        title: 'Phục hồi thành công',
        message: result.message || 'Dữ liệu đã được nạp và phục hồi vào hệ thống!',
      });
      setIsRestoreModalOpen(false);
      setRestoreJsonContent(null);
      setRestoreFileName('');
      fetchBackupSummary();
      fetchMetrics();
    } catch (err: any) {
      addNotification({
        type: 'error',
        title: 'Lỗi phục hồi dữ liệu',
        message: err.message || 'Quá trình phục hồi gặp sự cố.',
      });
    } finally {
      setRestoreLoading(false);
    }
  };

  return (
    <PageContainer
      title="Giám Sát Hệ Thống & Hiệu Năng API"
      subtitle="Đo lường thời gian đáp ứng (latency), tỷ lệ lỗi, thông lượng và tài nguyên hạ tầng"
      actions={
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 text-xs text-neutral-600">
            <span className="w-2 h-2 rounded-full bg-green-500"></span>
            Làm mới mỗi {refreshInterval / 1000}s
          </div>
          <Button
            variant="outline"
            size="sm"
            icon={<RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />}
            onClick={() => {
              setLoading(true);
              fetchMetrics();
            }}
          >
            Làm mới
          </Button>
        </div>
      }
    >
      <div className="space-y-6">

      {/* Overview Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">Trạng thái API</span>
            <Server className="w-4 h-4 text-orange-600" />
          </div>
          <div className="mt-2 flex items-baseline gap-2">
            <span className="text-2xl font-bold tracking-tight text-neutral-900">
              {metrics?.status || 'Online'}
            </span>
          </div>
          <div className="mt-2 flex items-center text-xs text-green-700 font-medium">
            <CheckCircle2 className="w-3.5 h-3.5 mr-1" />
            {metrics?.uptime ? `Uptime: ${metrics.uptime}` : 'Uptime: Đang kết nối...'}
          </div>
        </Card>

        <Card>
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">Thời gian phản hồi TB</span>
            <Clock className="w-4 h-4 text-orange-600" />
          </div>
          <div className="mt-2 flex items-baseline gap-2">
            <span className="text-2xl font-bold tracking-tight text-neutral-900">
              {metrics ? `${metrics.avgResponseTimeMs} ms` : 'Đang đo...'}
            </span>
          </div>
          <div className="mt-2 text-xs text-neutral-500">
            {metrics ? `Min: ${metrics.minResponseTimeMs}ms | Max: ${metrics.maxResponseTimeMs}ms` : 'Min / Max độ trễ'}
          </div>
        </Card>

        <Card>
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">Tổng Requests (.NET)</span>
            <Activity className="w-4 h-4 text-orange-600" />
          </div>
          <div className="mt-2 flex items-baseline gap-2">
            <span className="text-2xl font-bold tracking-tight text-neutral-900">
              {metrics ? metrics.totalRequests.toLocaleString('vi-VN') : '0'}
            </span>
          </div>
          <div className="mt-2 text-xs text-neutral-500">
            Tỷ lệ lỗi HTTP: {metrics?.errorRatePercent ?? 0}%
          </div>
        </Card>

        <Card>
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">MongoDB Cluster</span>
            <Database className="w-4 h-4 text-orange-600" />
          </div>
          <div className="mt-2 flex items-baseline gap-2">
            <span className="text-2xl font-bold tracking-tight text-neutral-900">
              {metrics?.mongoDb?.status || 'Đang kết nối...'}
            </span>
          </div>
          <div className="mt-2 text-xs text-neutral-500">
            {metrics?.mongoDb?.pingMs !== undefined ? `Ping: ${metrics.mongoDb.pingMs} ms (${metrics.mongoDb.databaseName})` : 'Đang kiểm tra kết nối...'}
          </div>
        </Card>
      </div>

      {/* Middle Section: Resource Utilization & Endpoints Breakdown */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Resource Consumption */}
        <Card title="Tài Nguyên Máy Chủ (Host & CLR Telemetry)">
          <div className="space-y-4">
            <div>
              <div className="flex justify-between text-xs font-medium text-neutral-700 mb-1">
                <span className="flex items-center gap-1.5">
                  <Cpu className="w-3.5 h-3.5 text-neutral-500" /> CPU Load (Host / Proc)
                </span>
                <span>{metrics?.cpuUsagePercent ?? 0}%</span>
              </div>
              <div className="w-full bg-neutral-100 h-2 rounded-none overflow-hidden">
                <div
                  className="bg-orange-600 h-2 transition-all duration-300"
                  style={{ width: `${Math.min(100, Math.max(0, metrics?.cpuUsagePercent ?? 0))}%` }}
                />
              </div>
            </div>

            <div>
              <div className="flex justify-between text-xs font-medium text-neutral-700 mb-1">
                <span>Bộ nhớ RAM (.NET WorkingSet & GC)</span>
                <span>{metrics?.memoryUsageMb ?? 0} MB (GC Heap: {metrics?.gcMemoryMb ?? 0} MB)</span>
              </div>
              <div className="w-full bg-neutral-100 h-2 rounded-none overflow-hidden">
                <div
                  className="bg-neutral-900 h-2 transition-all duration-300"
                  style={{ width: `${Math.min(100, ((metrics?.memoryUsageMb ?? 0) / 1024) * 100)}%` }}
                />
              </div>
            </div>

            <div>
              <div className="flex justify-between text-xs font-medium text-neutral-700 mb-1">
                <span>Luồng xử lý CLR (Active Threads)</span>
                <span>{metrics?.threadCount ?? 0} threads đang chạy</span>
              </div>
              <div className="w-full bg-neutral-100 h-2 rounded-none overflow-hidden">
                <div
                  className="bg-neutral-400 h-2 transition-all duration-300"
                  style={{ width: `${Math.min(100, ((metrics?.threadCount ?? 0) / 64) * 100)}%` }}
                />
              </div>
            </div>
          </div>

          <div className="mt-6 pt-4 border-t border-neutral-200 grid grid-cols-2 gap-4 text-xs">
            <div>
              <span className="text-neutral-500">Phiên bản Framework:</span>
              <p className="font-semibold text-neutral-900">{metrics?.framework || '.NET 8.0 ASP.NET Core'}</p>
            </div>
            <div>
              <span className="text-neutral-500">Giờ máy chủ:</span>
              <p className="font-semibold text-neutral-900 font-mono">
                {metrics?.serverTime ? new Date(metrics.serverTime).toLocaleTimeString('vi-VN') : 'Đang đồng bộ...'}
              </p>
            </div>
          </div>
        </Card>

        {/* Endpoints Latency Log */}
        <Card title="Hiệu Năng Các Điểm Cuối (Live Endpoints Telemetry)" noPadding>
          <div className="overflow-x-auto max-h-[290px] overflow-y-auto">
            <table className="w-full text-left text-xs text-neutral-700">
              <thead className="bg-neutral-50 font-semibold text-neutral-600 border-b border-neutral-200 sticky top-0 z-10">
                <tr>
                  <th className="py-2.5 px-4">Endpoint</th>
                  <th className="py-2.5 px-4">Method</th>
                  <th className="py-2.5 px-4 text-right">Lượt gọi</th>
                  <th className="py-2.5 px-4">Độ trễ TB</th>
                  <th className="py-2.5 px-4 text-right">Trạng thái</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-200">
                {metrics?.endpoints && metrics.endpoints.length > 0 ? (
                  metrics.endpoints.map((ep, idx) => (
                    <tr key={`${ep.endpoint}-${ep.method}-${idx}`} className="hover:bg-neutral-50/50">
                      <td className="py-2 px-4 font-mono font-medium text-neutral-900 max-w-[180px] truncate" title={ep.endpoint}>
                        {ep.endpoint}
                      </td>
                      <td className="py-2 px-4">
                        <Badge
                          variant={
                            ep.method === 'POST' ? 'orange' :
                            ep.method === 'GET' ? 'neutral' :
                            ep.method === 'PUT' ? 'warning' :
                            ep.method === 'DELETE' ? 'danger' : 'neutral'
                          }
                        >
                          {ep.method}
                        </Badge>
                      </td>
                      <td className="py-2 px-4 text-right font-mono text-neutral-600">
                        {ep.count.toLocaleString('vi-VN')}
                      </td>
                      <td className="py-2 px-4 font-medium">
                        <span className={ep.avgLatencyMs > 500 ? 'text-red-600' : ep.avgLatencyMs > 200 ? 'text-amber-600' : 'text-neutral-900'}>
                          {ep.avgLatencyMs} ms
                        </span>
                      </td>
                      <td className="py-2 px-4 text-right">
                        <Badge variant={ep.status.startsWith('2') ? 'success' : ep.status.startsWith('4') ? 'warning' : 'danger'}>
                          {ep.status}
                        </Badge>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5} className="py-8 text-center text-neutral-400">
                      Chưa ghi nhận lượt gọi API nào từ lúc khởi động. Hãy thao tác trong hệ thống để xem dữ liệu đo thời gian thực!
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
      </div>

      {/* Backup & Restore Management (Admin Only) */}
      <Card
        title="Sao Lưu & Phục Hồi Dữ Liệu Toàn Hệ Thống (Backup & Restore)"
        subtitle="Quản trị viên xuất toàn bộ các collection sang file JSON hoặc nạp bản sao lưu để phục hồi cơ sở dữ liệu"
        action={
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              icon={<RefreshCw className={`w-3.5 h-3.5 ${backupLoading ? 'animate-spin' : ''}`} />}
              onClick={fetchBackupSummary}
              disabled={backupLoading}
            >
              Cập nhật số liệu
            </Button>
            <Button
              variant="primary"
              size="sm"
              icon={<HardDriveDownload className="w-3.5 h-3.5" />}
              onClick={handleExportBackup}
              isLoading={backupLoading}
            >
              Xuất Sao Lưu (JSON)
            </Button>
            <Button
              variant="outline"
              size="sm"
              icon={<Upload className="w-3.5 h-3.5" />}
              onClick={() => setIsRestoreModalOpen(true)}
            >
              Phục Hồi Dữ Liệu
            </Button>
          </div>
        }
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
            {backupSummary?.collections ? (
              Object.entries(backupSummary.collections).map(([collectionName, count]) => {
                const labelMap: Record<string, string> = {
                  shops: 'Chi nhánh (shops)',
                  menuItems: 'Món thực đơn',
                  inventoryItems: 'Kho nguyên liệu',
                  orders: 'Đơn hàng (orders)',
                  staff: 'Nhân sự (staff)',
                  users: 'Tài khoản (users)',
                  reviews: 'Đánh giá (reviews)',
                };
                return (
                  <div key={collectionName} className="p-3 bg-neutral-50 border border-neutral-200 text-center">
                    <span className="text-[11px] font-semibold text-neutral-500 uppercase block truncate">
                      {labelMap[collectionName] || collectionName}
                    </span>
                    <span className="text-xl font-bold text-neutral-900 mt-1 block">
                      {count.toLocaleString('vi-VN')}
                    </span>
                    <span className="text-[10px] text-neutral-400 mt-0.5 block">bản ghi</span>
                  </div>
                );
              })
            ) : (
              <div className="col-span-full py-4 text-center text-xs text-neutral-500">
                Đang nạp thông tin sao lưu hệ thống...
              </div>
            )}
          </div>

          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between text-xs text-neutral-500 pt-2 border-t border-neutral-100 gap-2">
            <div>
              <span className="font-medium text-neutral-700">Cơ sở dữ liệu:</span>{' '}
              <span className="font-mono text-neutral-900">{backupSummary?.databaseName || 'cafe_management'}</span>
              <span className="mx-2">•</span>
              <span className="font-medium text-neutral-700">Thời điểm kiểm tra:</span>{' '}
              <span>{backupSummary?.timestamp ? new Date(backupSummary.timestamp).toLocaleString('vi-VN') : 'Mới nhất'}</span>
            </div>
            <div className="text-[11px] text-orange-700 bg-orange-50 px-2.5 py-1 rounded border border-orange-200">
              Mẹo: Xuất file sao lưu định kỳ trước khi thực hiện các đợt xóa hàng loạt hoặc bảo trì lớn.
            </div>
          </div>
        </div>
      </Card>

      {/* Log & Diagnostic stream */}
      <Card title="Nhật Ký Chẩn Đoán & Sự Kiện Realtime (Diagnostics Log)">
        <div className="bg-neutral-900 text-neutral-100 p-4 font-mono text-xs space-y-1.5 overflow-x-auto">
          <div>
            <span className="text-neutral-500">[{metrics?.serverTime ? new Date(metrics.serverTime).toISOString().replace('T', ' ').slice(0, 19) : new Date().toISOString().replace('T', ' ').slice(0, 19)} INF]</span>{' '}
            <span className="text-green-400">HOST:</span> {metrics?.framework || '.NET 8.0 ASP.NET Core'} | Uptime: {metrics?.uptime || 'N/A'} | Threads: {metrics?.threadCount ?? 0}
          </div>
          <div>
            <span className="text-neutral-500">[{metrics?.serverTime ? new Date(metrics.serverTime).toISOString().replace('T', ' ').slice(0, 19) : new Date().toISOString().replace('T', ' ').slice(0, 19)} INF]</span>{' '}
            <span className="text-amber-400">DATABASE:</span> MongoDB '{metrics?.mongoDb?.databaseName || 'cafe_management'}' (Ping: {metrics?.mongoDb?.pingMs ?? 0}ms, Status: {metrics?.mongoDb?.status || 'Unknown'})
          </div>
          <div>
            <span className="text-neutral-500">[{metrics?.serverTime ? new Date(metrics.serverTime).toISOString().replace('T', ' ').slice(0, 19) : new Date().toISOString().replace('T', ' ').slice(0, 19)} INF]</span>{' '}
            <span className="text-cyan-400">MEMORY:</span> WorkingSet: {metrics?.memoryUsageMb ?? 0} MB | GC Heap: {metrics?.gcMemoryMb ?? 0} MB | CPU Load: {metrics?.cpuUsagePercent ?? 0}%
          </div>
          {metrics?.endpoints && metrics.endpoints.length > 0 ? (
            metrics.endpoints.slice(0, 5).map((ep, idx) => (
              <div key={idx}>
                <span className="text-neutral-500">[{metrics?.serverTime ? new Date(metrics.serverTime).toISOString().replace('T', ' ').slice(0, 19) : new Date().toISOString().replace('T', ' ').slice(0, 19)} INF]</span>{' '}
                <span className="text-orange-400">TELEMETRY:</span> {ep.method} {ep.endpoint} &rarr; {ep.count} hits, avg {ep.avgLatencyMs}ms [HTTP {ep.status}]
              </div>
            ))
          ) : (
            <div>
              <span className="text-neutral-500">[{metrics?.serverTime ? new Date(metrics.serverTime).toISOString().replace('T', ' ').slice(0, 19) : new Date().toISOString().replace('T', ' ').slice(0, 19)} INF]</span>{' '}
              <span className="text-neutral-400">TELEMETRY:</span> Đang chờ các yêu cầu HTTP tiếp theo...
            </div>
          )}
        </div>
      </Card>

      {/* Restore Confirmation Modal */}
      <Modal
        isOpen={isRestoreModalOpen}
        onClose={() => {
          setIsRestoreModalOpen(false);
          setRestoreJsonContent(null);
          setRestoreFileName('');
        }}
        title="Phục Hồi Dữ Liệu Hệ Thống"
        description="Chọn file backup (.json) đã xuất trước đó để phục hồi lại các bảng dữ liệu"
        maxWidth="lg"
      >
        <div className="space-y-4">
          <div className="p-3 bg-amber-50 border border-amber-200 rounded text-xs text-amber-800 flex gap-2.5">
            <AlertTriangle className="w-5 h-5 flex-shrink-0 text-amber-600 mt-0.5" />
            <div>
              <p className="font-bold">Cảnh báo ghi đè dữ liệu!</p>
              <p className="mt-0.5 text-amber-700">
                Thao tác phục hồi sẽ đồng bộ và ghi đè các bản ghi có trùng ID trong file sao lưu vào cơ sở dữ liệu MongoDB hiện tại.
              </p>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-neutral-700 mb-1">
              Chọn tệp tin sao lưu (.json)
            </label>
            <input
              type="file"
              accept=".json"
              onChange={handleFileSelect}
              className="block w-full text-xs text-neutral-500 file:mr-4 file:py-2 file:px-4 file:rounded-none file:border-0 file:text-xs file:font-semibold file:bg-neutral-900 file:text-white hover:file:bg-cafe-orange file:cursor-pointer cursor-pointer border border-neutral-300"
            />
          </div>

          {restoreJsonContent && (
            <div className="p-3 bg-neutral-50 border border-neutral-200 rounded text-xs space-y-2">
              <div className="flex items-center gap-2 font-semibold text-neutral-900">
                <FileJson className="w-4 h-4 text-orange-600" />
                <span>Tệp tin: {restoreFileName}</span>
              </div>
              <p className="text-neutral-600">
                Thời điểm sao lưu:{' '}
                <span className="font-mono font-medium">
                  {restoreJsonContent.exportedAt
                    ? new Date(restoreJsonContent.exportedAt).toLocaleString('vi-VN')
                    : 'Không xác định'}
                </span>
              </p>
              <div className="grid grid-cols-3 gap-2 pt-1 font-mono text-[11px]">
                {restoreJsonContent.collections &&
                  Object.entries(restoreJsonContent.collections).map(([key, items]: [string, any]) => (
                    <div key={key} className="bg-white p-1.5 border border-neutral-200">
                      <span className="text-neutral-500 block truncate">{key}:</span>
                      <span className="font-bold text-neutral-900">
                        {Array.isArray(items) ? items.length : 0} mục
                      </span>
                    </div>
                  ))}
              </div>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-3 border-t border-neutral-200">
            <Button
              variant="outline"
              onClick={() => {
                setIsRestoreModalOpen(false);
                setRestoreJsonContent(null);
                setRestoreFileName('');
              }}
              disabled={restoreLoading}
            >
              Hủy bỏ
            </Button>
            <Button
              variant="danger"
              icon={<Upload className="w-4 h-4" />}
              onClick={handleExecuteRestore}
              disabled={!restoreJsonContent || restoreLoading}
              isLoading={restoreLoading}
            >
              Bắt đầu phục hồi
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  </PageContainer>
  );
};



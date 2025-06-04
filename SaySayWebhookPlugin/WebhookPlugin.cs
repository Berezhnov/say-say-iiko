using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Resto.Front.Api;
using Resto.Front.Api.Attributes;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Organization;

namespace SaySay.IikoWebhookPlugin
{
    /// <summary>
    /// Плагин для отправки вебхуков при изменении заказов и доставок
    /// </summary>
    [PluginLicenseModuleId(21005108)]
    [UsedImplicitly]
    public sealed class WebhookPlugin : IFrontPlugin
    {
        private const string WEBHOOK_URL = "https://api.say-say.ru/api/iikotest";
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly Stack<IDisposable> subscriptions = new Stack<IDisposable>();

        public WebhookPlugin()
        {
            PluginContext.Log.Info("Say-Say Webhook Plugin: Инициализация");
        }

        public void Dispose()
        {
            PluginContext.Log.Info("Say-Say Webhook Plugin: Завершение работы");

            while (subscriptions.Any())
            {
                var subscription = subscriptions.Pop();
                try
                {
                    subscription.Dispose();
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Error($"Ошибка при отписке: {ex.Message}");
                }
            }

            httpClient?.Dispose();
        }

        public void Init(IPluginIntegrationService pluginIntegrationService)
        {
            PluginContext.Log.Info("Say-Say Webhook Plugin: Запуск");

            // Подписка на изменения заказов
            subscriptions.Push(
                PluginContext.Notifications.OrderChanged.Subscribe(OnOrderChanged)
            );

            // Подписка на изменения доставок
            subscriptions.Push(
                PluginContext.Notifications.DeliveryOrderChanged.Subscribe(OnDeliveryOrderChanged)
            );

            PluginContext.Log.Info("Say-Say Webhook Plugin: Подписки активированы");
        }

        /// <summary>
        /// Обработчик изменений заказов
        /// </summary>
        private void OnOrderChanged(IOrder order)
        {
            try
            {
                PluginContext.Log.Info($"Изменение заказа: {order.Number}, статус: {order.Status}");

                var webhookData = new OrderWebhookData
                {
                    EventType = DetermineEventType(order),
                    EntityType = "order",
                    OrderId = order.Id.ToString(),
                    OrderNumber = order.Number.ToString(),
                    Status = order.Status.ToString(),
                    TableNumber = order.Table?.Number.ToString() ?? "N/A",
                    Sum = order.GetCost(),
                    Items = order.Guests.SelectMany(g => g.Items).Select(item => new OrderItemData
                    {
                        Name = item.Product.Name,
                        Amount = item.Amount,
                        Price = item.Price,
                        Sum = item.Cost
                    }).ToList(),
                    CreatedAt = order.OpenTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                    Timestamp = DateTime.Now
                };

                SendWebhookAsync(webhookData).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"Ошибка обработки изменения заказа: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик изменений доставок
        /// </summary>
        private void OnDeliveryOrderChanged(IDeliveryOrder deliveryOrder, IRestaurant restaurant)
        {
            try
            {
                PluginContext.Log.Info($"Изменение доставки: {deliveryOrder.Number}, статус: {deliveryOrder.Status}");

                var webhookData = new DeliveryWebhookData
                {
                    EventType = DetermineDeliveryEventType(deliveryOrder),
                    EntityType = "delivery",
                    DeliveryId = deliveryOrder.Id.ToString(),
                    OrderNumber = deliveryOrder.Number.ToString(),
                    Status = deliveryOrder.Status.ToString(),
                    DeliveryStatus = deliveryOrder.DeliveryStatus.ToString(),
                    CustomerName = deliveryOrder.Customer?.Name ?? "N/A",
                    CustomerPhone = deliveryOrder.Customer?.Phone ?? "N/A",
                    Address = deliveryOrder.Address?.Line1 ?? "N/A",
                    Sum = deliveryOrder.GetCost(),
                    Items = deliveryOrder.Guests.SelectMany(g => g.Items).Select(item => new OrderItemData
                    {
                        Name = item.Product.Name,
                        Amount = item.Amount,
                        Price = item.Price,
                        Sum = item.Cost
                    }).ToList(),
                    CreatedAt = deliveryOrder.OpenTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                    DeliveryDate = deliveryOrder.DeliveryDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                    Timestamp = DateTime.Now
                };

                SendWebhookAsync(webhookData).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"Ошибка обработки изменения доставки: {ex.Message}");
            }
        }

        /// <summary>
        /// Определение типа события для заказа
        /// </summary>
        private string DetermineEventType(IOrder order)
        {
            if (order.Status == OrderStatus.Closed || order.Status == OrderStatus.Deleted)
                return "deleted";

            if (order.Status == OrderStatus.New && order.OpenTime?.AddSeconds(5) > DateTime.Now)
                return "created";

            return "status_changed";
        }

        /// <summary>
        /// Определение типа события для доставки
        /// </summary>
        private string DetermineDeliveryEventType(IDeliveryOrder deliveryOrder)
        {
            if (deliveryOrder.Status == OrderStatus.Closed || deliveryOrder.Status == OrderStatus.Deleted)
                return "deleted";

            if (deliveryOrder.Status == OrderStatus.New && deliveryOrder.OpenTime?.AddSeconds(5) > DateTime.Now)
                return "created";

            return "status_changed";
        }

        /// <summary>
        /// Асинхронная отправка вебхука
        /// </summary>
        private async Task SendWebhookAsync(object webhookData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(webhookData, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                PluginContext.Log.Info($"Отправка вебхука: {json}");

                var response = await httpClient.PostAsync(WEBHOOK_URL, content);

                if (response.IsSuccessStatusCode)
                {
                    PluginContext.Log.Info($"Вебхук успешно отправлен. Статус: {response.StatusCode}");
                }
                else
                {
                    PluginContext.Log.Error($"Ошибка отправки вебхука. Статус: {response.StatusCode}, Ответ: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"Исключение при отправке вебхука: {ex.Message}");
            }
        }
    }

    #region Data Models

    /// <summary>
    /// Базовая модель данных вебхука
    /// </summary>
    public abstract class BaseWebhookData
    {
        public string EventType { get; set; }
        public string EntityType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Модель данных вебхука для заказа
    /// </summary>
    public class OrderWebhookData : BaseWebhookData
    {
        public string OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string Status { get; set; }
        public string TableNumber { get; set; }
        public decimal Sum { get; set; }
        public List<OrderItemData> Items { get; set; }
        public string CreatedAt { get; set; }
    }

    /// <summary>
    /// Модель данных вебхука для доставки
    /// </summary>
    public class DeliveryWebhookData : BaseWebhookData
    {
        public string DeliveryId { get; set; }
        public string OrderNumber { get; set; }
        public string Status { get; set; }
        public string DeliveryStatus { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string Address { get; set; }
        public decimal Sum { get; set; }
        public List<OrderItemData> Items { get; set; }
        public string CreatedAt { get; set; }
        public string DeliveryDate { get; set; }
    }

    /// <summary>
    /// Модель данных позиции заказа
    /// </summary>
    public class OrderItemData
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
        public decimal Sum { get; set; }
    }

    #endregion
}

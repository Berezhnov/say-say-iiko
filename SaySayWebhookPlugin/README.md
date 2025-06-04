# Say-Say Webhook Plugin для iiko Front

Дистрибутив Front https://downloads.iiko.online/8.9.7015.0/iiko/RMS/Front/Setup.Front.exe
Дистрибутив BackOffice https://downloads.iiko.online/8.9.7015.0/iiko/RMS/BackOffice/Setup.RMS.BackOffice.exe

## Описание

Плагин отправляет вебхуки на сервер Say-Say при изменении заказов и доставок в iiko Front.

### Отслеживаемые события:
- **Создание** заказа/доставки
- **Изменение статуса** заказа/доставки
- **Удаление** заказа/доставки

### Данные, отправляемые в вебхуке:

#### Для заказов:
- ID и номер заказа
- Статус заказа
- Номер стола
- Сумма заказа
- Список позиций (название, количество, цена, сумма)
- Время создания

#### Для доставок:
- ID и номер доставки
- Статус заказа и доставки
- Имя и телефон клиента
- Адрес доставки
- Сумма заказа
- Список позиций
- Время создания и время доставки

## Требования

- iiko Front версии 8.0 или выше
- .NET Framework 4.8
- Visual Studio 2019/2022 для компиляции

## Установка

### 1. Компиляция плагина

1. Откройте проект в Visual Studio
2. В файле `SaySay.IikoWebhookPlugin.csproj` укажите правильный путь к iiko Front:
   ```xml
   <IikoFrontPath>C:\Program Files\iiko\iikoRMS\Front.Net</IikoFrontPath>
   ```
3. Выполните сборку проекта (Build → Build Solution)

### 2. Установка плагина

#### Автоматическая установка (через Post-Build Event):
При успешной сборке плагин автоматически скопируется в папку плагинов iiko.

#### Ручная установка:
1. Создайте папку `SaySay.IikoWebhookPlugin` в директории плагинов iiko:
   ```
   C:\Program Files\iiko\iikoRMS\Front.Net\Plugins\SaySay.IikoWebhookPlugin\
   ```

2. Скопируйте в созданную папку следующие файлы:
    - `SaySay.IikoWebhookPlugin.dll`
    - `manifest.xml`
    - `Newtonsoft.Json.dll`

### 3. Активация плагина

1. Запустите iiko Office
2. Перейдите в раздел **Настройки → Лицензии**
3. Найдите модуль с ID `21005108` и активируйте его
4. Перезапустите iiko Front

## Настройка

### Изменение URL вебхука

URL вебхука задан в коде как константа:
```csharp
private const string WEBHOOK_URL = "https://api.say-say.ru/api/iikotest";
```

Для изменения URL необходимо:
1. Изменить значение константы в коде
2. Пересобрать плагин
3. Переустановить плагин

### Формат вебхука

Вебхуки отправляются методом POST с Content-Type: application/json.

Пример данных для заказа:
```json
{
  "EventType": "created",
  "EntityType": "order",
  "OrderId": "550e8400-e29b-41d4-a716-446655440000",
  "OrderNumber": "123",
  "Status": "New",
  "TableNumber": "5",
  "Sum": 1500.00,
  "Items": [
    {
      "Name": "Пицца Маргарита",
      "Amount": 1,
      "Price": 500.00,
      "Sum": 500.00
    }
  ],
  "CreatedAt": "2024-01-15 14:30:00",
  "Timestamp": "2024-01-15T14:30:00"
}
```

Пример данных для доставки:
```json
{
  "EventType": "status_changed",
  "EntityType": "delivery",
  "DeliveryId": "660e8400-e29b-41d4-a716-446655440000",
  "OrderNumber": "456",
  "Status": "BillPrinted",
  "DeliveryStatus": "OnWay",
  "CustomerName": "Иван Иванов",
  "CustomerPhone": "+7 900 123-45-67",
  "Address": "ул. Пушкина, д. 10",
  "Sum": 2000.00,
  "Items": [...],
  "CreatedAt": "2024-01-15 13:00:00",
  "DeliveryDate": "2024-01-15 14:00:00",
  "Timestamp": "2024-01-15T13:30:00"
}
```

## Логирование

Плагин записывает логи в стандартный лог iiko Front. Просмотреть логи можно в:
```
C:\ProgramData\iiko\iikoRMS\Front.Net\Logs\
```

## Возможные проблемы

### Плагин не появляется в списке
- Проверьте правильность структуры папок
- Убедитесь, что manifest.xml находится в папке с плагином
- Проверьте версию iiko Front (должна быть 8.0+)

### Вебхуки не отправляются
- Проверьте доступность URL вебхука
- Проверьте логи на наличие ошибок
- Убедитесь, что плагин активирован в лицензиях

### Ошибки компиляции
- Проверьте путь к iiko Front в файле проекта
- Убедитесь, что установлен .NET Framework 4.8
- Проверьте наличие Resto.Front.Api.dll

## Дополнительные возможности

### Добавление QR-кода на чек

Для добавления QR-кода с ссылкой на отзыв необходимо:

1. Подписаться на событие печати чека:
```csharp
PluginContext.Notifications.BeforeOrderBill.Subscribe(OnBeforeOrderBill);
```

2. Добавить QR-код в чек:
```csharp
private void OnBeforeOrderBill(IOrder order, ICheque cheque)
{
    var reviewUrl = $"https://say-say.ru/review/{order.Id}";
    cheque.AddQRCode(reviewUrl, "Оставьте отзыв");
}
```

## Поддержка

При возникновении вопросов обращайтесь в службу поддержки Say-Say.

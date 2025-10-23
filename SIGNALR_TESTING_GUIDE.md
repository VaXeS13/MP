# SignalR Integration Testing Guide

## Overview
This guide provides comprehensive testing procedures for all 5 SignalR hubs in the MP application. All hubs are now fully integrated and operational.

## Hub Status

| Hub | Frontend | Backend | Status | Priority |
|-----|----------|---------|--------|----------|
| **ChatHub** | ✅ chat.service.ts | ✅ ChatHub.cs | 100% | Production |
| **NotificationHub** | ✅ notification.service.ts | ✅ NotificationHub.cs | 100% | Production |
| **BoothHub** | ✅ booth-signalr.service.ts | ✅ BoothHub.cs | 100% | Production |
| **DashboardHub** | ✅ dashboard-signalr.service.ts | ✅ DashboardHub.cs | 100% | Production |
| **SalesHub** | ✅ sales-signalr.service.ts | ✅ SalesHub.cs | 100% | Production |

---

## 1. ChatHub Testing (100% Complete)

### Test Case 1.1: User Connection
**Expected**: User connects to chat hub on app load
```
Steps:
1. Login as user
2. Open browser console
3. Filter logs: "SignalR: ✅ BoothHub connected"
4. Verify chatConnection state is Connected

Expected Output:
AppComponent: SignalR connections established
ChatService: Initialized and listening for messages
```

### Test Case 1.2: Send and Receive Messages
**Expected**: Messages sent are received in real-time
```
Steps:
1. Open 2 browser tabs with different users
2. User A sends message to User B
3. User B receives message instantly
4. Check message appears in chat window

Expected Output:
Backend: [SignalR] Sending message to user
Frontend: ChatService: ✅ Received ReceiveMessage event
```

### Test Case 1.3: Typing Indicator
**Expected**: Typing indicator appears/disappears
```
Steps:
1. User A starts typing in message input
2. User B sees "User A is typing..." indicator
3. User A sends message - indicator disappears

Expected Output:
SalesHub.on('UserTyping', { userId, isTyping: true })
```

---

## 2. NotificationHub Testing (100% Complete)

### Test Case 2.1: Notification Reception
**Expected**: User receives notifications in real-time
```
Steps:
1. Login as user
2. Trigger notification event (e.g., item sold)
3. Notification appears as toast
4. Check notification center

Expected Output:
NotificationService: ✅ Received ReceiveNotification event
MessageService shows toast with success/warning/error
```

### Test Case 2.2: Unread Count Update
**Expected**: Unread notification count updates correctly
```
Steps:
1. Open notification center
2. Trigger 3 notifications
3. Check unread count increases to 3
4. Mark all as read
5. Check unread count becomes 0

Expected Output:
UnreadCountUpdated: 3
UnreadCountUpdated: 0
```

### Test Case 2.3: Mark as Read
**Expected**: Notifications marked as read stop showing as unread
```
Steps:
1. Receive notification
2. Click notification to open
3. System marks as read
4. Unread count decreases

Expected Output:
Backend: [SignalR] Sending unread count update
Frontend: unreadCount$ = 0
```

---

## 3. BoothHub Testing (100% Complete)

### Test Case 3.1: Booth Status Update (Tenant-Wide)
**Expected**: All connected users see booth status changes
```
Steps:
1. Admin changes booth A status from Available → Rented
2. User B viewing floor plan sees booth A turn red immediately
3. User C also sees the change in real-time

Expected Output:
Backend: [SignalR] Sending booth status update to tenant
Frontend: BoothSignalRService: ✅ Received BoothStatusUpdated
FloorPlanView: Refreshing floor plan due to booth update
```

### Test Case 3.2: Floor-Plan Subscription
**Expected**: User only gets updates for their selected floor plan
```
Steps:
1. User selects Floor Plan A
2. Booth in Floor Plan A changes status
3. User receives update

Then:
4. User switches to Floor Plan B
5. Previous subscription to Floor Plan A is cancelled
6. Booth in Floor Plan A changes again
7. User does NOT receive update (only receives updates for Floor Plan B)

Expected Output:
FloorPlanView: Subscribing to floor plan updates for: [GUID]
FloorPlanView: ✅ Subscribed to floor plan: [GUID]
(When switching)
FloorPlanView: Unsubscribing from floor plan updates
FloorPlanView: ✅ Unsubscribed from floor plan
```

### Test Case 3.3: Component Cleanup
**Expected**: Subscriptions are cleaned up when component destroys
```
Steps:
1. User views floor plan
2. Navigate away from floor plan page
3. Check browser memory - no hanging subscriptions

Expected Output:
FloorPlanView.ngOnDestroy():
- unsubscribeFromFloorPlanUpdates() called
- destroy$.next() and destroy$.complete() called
- canvas.dispose() called
```

---

## 4. DashboardHub Testing (100% Complete) - NEW

### Test Case 4.1: Dashboard Live Update with Data
**Expected**: Dashboard metrics update in real-time WITHOUT full reload
```
Steps:
1. Login as admin
2. Open Dashboard
3. Create a new rental (booth gets rented)
4. Watch dashboard metrics update immediately
   - TotalRentals increases
   - OccupiedBooths increases
   - OccupancyRate recalculates

Expected Output:
Backend: [SignalR] Sending dashboard update with data to tenant
Frontend: DashboardSignalRService: ✅ Received DashboardUpdated with data
Dashboard: ✅ Received dashboard data update via SignalR
(Metrics update without full page reload)
```

### Test Case 4.2: Dashboard Refresh Trigger (Fallback)
**Expected**: Full refresh trigger works when partial updates insufficient
```
Steps:
1. Dashboard receiving updates
2. Complex data change (multiple updates needed)
3. Backend sends DashboardRefreshNeeded
4. Frontend reloads all dashboard metrics

Expected Output:
Backend: [SignalR] Sending dashboard refresh to tenant
Frontend: DashboardSignalRService: ✅ Received DashboardRefreshNeeded
Dashboard: ✅ Refresh triggered by SignalR
loadDashboardData() called
```

### Test Case 4.3: Multiple Admin View Sync
**Expected**: All admins see same dashboard data in real-time
```
Steps:
1. Admin A views Dashboard
2. Admin B views Dashboard
3. Admin A creates rental
4. Both Admin A and Admin B see metrics update simultaneously

Expected Output:
(Both admins in dashboard:tenant:{tenantId} group)
Both receive: DashboardUpdated event with same data
Both dashboards display identical metrics
```

---

## 5. SalesHub Testing (100% Complete) - NEW

### Test Case 5.1: Item Sold Notification
**Expected**: User receives ItemSold event when item sells
```
Steps:
1. User A has items on sale
2. Cashier checks out one of User A's items
3. User A receives ItemSold notification in SalesHub

Expected Output:
Backend: [SignalR] Sending item sold notification to user
Backend: [SignalR] Item sold event sent to sales hub for user
Frontend: SalesSignalRService: ✅ Received ItemSold event
ItemSold contains: itemId, itemName, salePrice, soldAt, rentalId
```

### Test Case 5.2: Rental-Specific Sales Updates
**Expected**: Subscribed rental receives ItemSold events
```
Steps:
1. User subscribes to Rental A sales: subscribeToRentalSales(rentalId)
2. Item from Rental A is sold
3. User receives ItemSold event

Expected Output:
Backend: Item sold event sent to rental sales group for rental [GUID]
Frontend: rentalSalesUpdatedSubject$ emits update
Dashboard shows: Item X sold from Rental A
```

### Test Case 5.3: User vs Rental Group Targeting
**Expected**: Events go to correct groups
```
Steps:
1. Item from Rental A (User X's item) is sold
2. Verify event is sent to:
   - sales:user:X (User X receives)
   - sales:rental:A (Rental A subscribers receive)
   - Sales:tenant:T (Optional: admin visibility)

Expected Output:
[SignalR] Item sold event sent to sales hub for user [X]
[SignalR] Item sold event sent to rental sales group for rental [A]
```

---

## Browser Console Testing

### Enable SignalR Logging
```javascript
// Open browser console and run:
window.localStorage.setItem('loglevel:ms', 'debug');
```

### Check Connection Status
```javascript
// In console, check each hub:
app.service.signalRService.notificationHub.state
app.service.signalRService.dashboardHub.state
app.service.signalRService.boothHub.state
app.service.signalRService.salesHub.state
app.service.signalRService.chatHub.state

// Expected: HubConnectionState.Connected (1)
```

### Monitor Incoming Events
```javascript
// Example: Monitor ChatHub messages
const chatHub = app.service.signalRService.chatHub;
chatHub.on('ReceiveMessage', (msg) => {
  console.log('Message received:', msg);
});
```

---

## Performance Testing

### Test Case: Connection Stability
**Expected**: All 5 hubs maintain connection under load
```
Steps:
1. Keep dashboard open for 1 hour
2. Generate continuous booth updates (every 5 seconds)
3. Check:
   - No memory leaks
   - No dropped messages
   - Connection remains stable

Success Criteria:
- Browser memory stable (no continuous growth)
- All messages received (0 lost)
- No reconnection attempts
```

### Test Case: Reconnection Handling
**Expected**: Hubs reconnect automatically after network interruption
```
Steps:
1. Disconnect network (DevTools → Offline)
2. Wait 30 seconds
3. Reconnect network
4. Verify all hubs reconnect

Expected Output:
SignalR: Reconnecting to [hub-url]
SignalR: ✅ Reconnected to [hub-url]
```

---

## Troubleshooting Checklist

### If Hub Not Connecting
```
[ ] Check if API server is running (https://localhost:44377)
[ ] Check if authentication token is valid
[ ] Check browser console for CORS errors
[ ] Verify hub endpoint: /signalr-hubs/[hubname]
[ ] Check browser DevTools → Network → WS (WebSocket)
```

### If Messages Not Received
```
[ ] Check if hub is properly subscribed to groups
[ ] Verify user is in correct tenant context
[ ] Check if sender is in correct group
[ ] Check server logs for [SignalR] errors
[ ] Verify no exceptions in SignalR methods
```

### If Performance Issues
```
[ ] Monitor browser DevTools → Performance tab
[ ] Check memory usage under constant updates
[ ] Verify no console errors
[ ] Check subscription cleanup in ngOnDestroy
[ ] Reduce update frequency if necessary
```

---

## Implementation Checklist

- [x] ChatHub frontend and backend
- [x] NotificationHub frontend and backend
- [x] BoothHub frontend and backend
- [x] BoothHub floor-plan subscriptions
- [x] DashboardHub with data updates
- [x] SalesHub with ItemSold events
- [x] Proper memory management (takeUntil)
- [x] Error handling and logging
- [x] Type-safe DTOs
- [x] Multi-tenant isolation

---

## Related Files

### Backend
- `src/MP.HttpApi.Host/Hubs/ChatHub.cs`
- `src/MP.HttpApi.Host/Hubs/NotificationHub.cs`
- `src/MP.HttpApi.Host/Hubs/BoothHub.cs`
- `src/MP.HttpApi.Host/Hubs/DashboardHub.cs`
- `src/MP.HttpApi.Host/Hubs/SalesHub.cs`
- `src/MP.HttpApi.Host/Services/SignalRNotificationService.cs`

### Frontend
- `angular/src/app/services/signalr.service.ts` (core connection)
- `angular/src/app/services/chat.service.ts`
- `angular/src/app/services/notification.service.ts`
- `angular/src/app/services/booth-signalr.service.ts`
- `angular/src/app/services/dashboard-signalr.service.ts` (NEW)
- `angular/src/app/services/sales-signalr.service.ts` (NEW)
- `angular/src/app/app.component.ts` (initialization)
- `angular/src/app/floor-plan/floor-plan-view.component.ts` (floor-plan subscriptions)

---

## Conclusion

All 5 SignalR hubs are now fully implemented with:
- ✅ Real-time communication
- ✅ Proper group-based targeting
- ✅ Memory leak prevention
- ✅ Error handling
- ✅ Comprehensive logging
- ✅ Type safety

The system is production-ready for real-time features including chat, notifications, booth status updates, dashboard metrics, and sales notifications.

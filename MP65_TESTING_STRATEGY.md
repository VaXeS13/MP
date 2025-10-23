# MP-65: End-to-End Testing Strategy

## Overview

MP-65 is for implementing comprehensive end-to-end tests for the local agent integration (MP-60, MP-61, MP-62, MP-63, MP-64). This document outlines the testing strategy and test cases required to validate the complete sales flow from UI through local agent device communication.

## Testing Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Unit Tests (Domain + Application Layer)                      │
├─────────────────────────────────────────────────────────────┤
│ Integration Tests (ItemCheckoutAppService + IRemoteDeviceProxy) │
├─────────────────────────────────────────────────────────────┤
│ E2E Tests (Full checkout flow + Device simulation)          │
├─────────────────────────────────────────────────────────────┤
│ Performance Tests (Load testing, latency monitoring)        │
├─────────────────────────────────────────────────────────────┤
│ Multi-Tenant Tests (Isolation, security, independent ops)  │
└─────────────────────────────────────────────────────────────┘
```

## Test Fixtures and Mocking Strategy

### LocalAgentTestFixture
- **Purpose**: Simulate local agent device responses
- **Responsibilities**:
  - Mock `IRemoteDeviceProxy` interface
  - Simulate device online/offline states
  - Track executed commands
  - Simulate network delays
  - Provide device status responses

### Implementation Pattern
```csharp
// Setup in test class
protected override void AfterAddApplication(IServiceCollection services)
{
    _fixture = new LocalAgentTestFixture();
    services.AddSingleton(_fixture.DeviceProxy);
}

// Simulate device states
_fixture.SimulateDeviceOnline("terminal");
_fixture.SimulateDeviceOffline("fiscal_printer");
_fixture.SimulateDeviceDelay("terminal", TimeSpan.FromSeconds(2));

// Track operations
var commandCount = _fixture.GetCommandCount("Authorize");
```

## Test Categories

### 1. Basic Flow Validation Tests

**Test Cases**:
- ✅ Full checkout flow with card payment
- ✅ Cash checkout (doesn't use terminal)
- ✅ Multi-item checkout
- ✅ Payment authorization and capture
- ✅ Fiscal receipt printing after payment

**Key Validations**:
- All steps execute successfully
- Correct state transitions
- Data persistence
- No data loss during operations
- Proper error messaging

### 2. Device Availability Tests

**Test Cases**:
- ✅ Terminal online → payment methods available
- ✅ Terminal offline → card payment unavailable
- ✅ Fiscal printer online → receipt printing available
- ✅ Fiscal printer offline → payment still works
- ✅ Both devices available → all options available
- ✅ Both devices offline → cash only

**Key Validations**:
- Correct device status detection
- Proper availability reporting
- No blocking when optional devices offline

### 3. Error Scenario Tests

**Test Cases**:
- ✅ Agent offline during transaction
- ✅ Terminal offline during payment
- ✅ Fiscal printer offline during receipt printing
- ✅ Network connectivity loss
- ✅ Device timeout
- ✅ Command failure and retry
- ✅ Partial failure recovery

**Key Validations**:
- Graceful error handling
- Proper error messages to user
- Transaction state preserved
- Retry logic executes correctly
- No data corruption

### 4. Performance Tests

**Test Cases**:
- ✅ Single checkout <2 seconds
- ✅ 10 concurrent checkouts complete within 5 seconds
- ✅ Device status check <500ms
- ✅ Payment authorization <2 seconds
- ✅ Fiscal receipt printing <3 seconds
- ✅ Memory usage stable under load
- ✅ Connection pool usage optimal

**Key Validations**:
- Response times acceptable
- No memory leaks
- Connection pooling works
- Handles concurrent operations
- Graceful degradation under load

### 5. Multi-Tenant Tests

**Test Cases**:
- ✅ Tenant A and B have independent device status
- ✅ Tenant A payment doesn't affect Tenant B
- ✅ Each tenant can configure different devices
- ✅ Cross-tenant data isolation enforced
- ✅ Commands execute in correct tenant context
- ✅ Device recovery independent per tenant

**Key Validations**:
- Tenant context properly set on all operations
- No data leakage between tenants
- Independent device management
- Isolated error handling
- Proper resource cleanup per tenant

### 6. Recovery and Resilience Tests

**Test Cases**:
- ✅ Device recovers after offline period
- ✅ Queue preserves commands during disconnection
- ✅ Automatic reconnection on network restore
- ✅ Transaction continues after temporary failure
- ✅ Graceful degradation when optional services unavailable
- ✅ Circuit breaker pattern for failed devices

**Key Validations**:
- Automatic recovery without user intervention
- Queue integrity maintained
- No duplicate command execution
- Transaction state consistency

### 7. Security and Authorization Tests

**Test Cases**:
- ✅ Tenant A cannot execute commands for Tenant B
- ✅ Unauthorized users cannot access device operations
- ✅ Command signatures validated
- ✅ No cross-tenant data exposure
- ✅ Audit trail captured for all device operations

**Key Validations**:
- Proper authorization checks
- Tenant isolation enforced
- No privilege escalation
- Complete audit trail

## Test Implementation Checklist

### Phase 1: Unit Tests
- [ ] Domain service tests for payment processing
- [ ] Item checkout service logic tests
- [ ] Device proxy interface contract tests
- [ ] Command/Response serialization tests

### Phase 2: Integration Tests
- [ ] ItemCheckoutAppService with mock proxy
- [ ] Device status queries
- [ ] Payment authorization flow
- [ ] Fiscal receipt generation
- [ ] Error handling scenarios
- [ ] Multi-tenant isolation

### Phase 3: E2E Tests
- [ ] Full checkout flow (UI → API → Agent → Device)
- [ ] Device failure scenarios
- [ ] Recovery flows
- [ ] Performance validation
- [ ] Load testing

### Phase 4: Regression Tests
- [ ] Backward compatibility with non-agent payments
- [ ] Cash checkout still works
- [ ] Existing payment flows unaffected
- [ ] No breaking changes to public APIs

## Mock Device Implementation Strategy

```csharp
public class MockDeviceProxy : IRemoteDeviceProxy
{
    private Dictionary<string, DeviceState> _devices = new();

    public async Task<TerminalPaymentResponse> AuthorizePaymentAsync(...)
    {
        // Simulate device response
        // Include configurable delays
        // Track command execution
        // Support failure scenarios
    }
}
```

## Test Data Management

**Test Fixtures**:
- Multiple test tenants (A, B, C)
- Various device configurations
- Different payment amounts
- Items with various prices
- Booth configurations per tenant

**Data Cleanup**:
- Automatic cleanup after each test
- Transaction rollback on test failure
- No test data pollution
- Isolated database per test class

## Performance Baselines

| Operation | Target | Acceptable Range |
|-----------|--------|------------------|
| Device status check | <500ms | <1s |
| Terminal payment | <2s | <5s |
| Fiscal receipt print | <3s | <5s |
| Full checkout | <5s | <10s |
| Concurrent (10) checkout | <5s each | <15s total |

## Monitoring and Metrics

**Performance Metrics**:
- API response times per endpoint
- SignalR message latency
- Device command execution time
- Queue depth and processing rate
- Memory and CPU usage
- Connection pool utilization

**Business Metrics**:
- Transaction success rate
- Payment authorization success rate
- Fiscal receipt printing success rate
- Error rate by error type
- Device availability uptime

**Technical Metrics**:
- Connection stability
- Device reconnection time
- Command retry success rate
- Resource cleanup effectiveness

## Test Coverage Goals

- **Domain Layer**: 95%+ coverage
- **Application Layer**: 90%+ coverage
- **Device Integration**: 85%+ coverage
- **Error Paths**: 100% coverage
- **Critical Flows**: 100% coverage

## Continuous Integration

**Test Execution**:
- Run all unit tests on every commit
- Run integration tests before merge
- Run E2E tests in staging environment
- Performance tests on release candidates
- Nightly load testing

**Test Failure Handling**:
- Automatic issue creation for failures
- Retry flaky tests 3 times
- Skip tests marked as @Skip with reason
- Report coverage metrics
- Archive test logs

## Known Limitations and Future Work

### Current Limitations
1. **Response DTO Properties**: Some response types may be missing properties used in tests
   - Solution: Update test fixtures to match actual response structures
   - Fix: Review `LocalAgent.Contracts.Responses` classes

2. **Test Fixture Complexity**: Mock device simulation can be complex
   - Solution: Start with simple mocks, enhance gradually
   - Fix: Create helper builders for common test scenarios

3. **Multi-Tenant Testing**: Requires careful context management
   - Solution: Use ICurrentTenant.Change() with using blocks
   - Fix: Create reusable tenant test utilities

### Future Enhancements
1. Docker-based test environment with real devices
2. Performance regression detection in CI
3. Contract testing with actual Local Agent service
4. Chaos engineering tests for resilience
5. A/B testing framework for payment flows

## Implementation Steps

1. **Review response DTOs**: Verify all expected properties exist in contract types
2. **Create working test fixture**: Start with simple mock implementations
3. **Implement basic flow tests**: Validate happy path scenarios
4. **Add error scenario tests**: Cover exception cases
5. **Performance testing**: Measure and validate response times
6. **Multi-tenant tests**: Verify isolation and security
7. **CI/CD integration**: Automate test execution
8. **Documentation**: Create runbook for test maintenance

## References

- MP-60: Local Agent MP Implementation
- MP-61: SignalR Hub in HttpApi
- MP-62: IRemoteDeviceProxy Implementation
- MP-63: ItemCheckoutAppService Refactoring
- MP-64: MP.LocalAgent Core Services
- LocalAgent.Contracts: Command/Response types
- IRemoteDeviceProxy: Interface contract

## Success Criteria

✅ All basic flow tests passing
✅ Error scenarios handled gracefully
✅ Performance baselines met
✅ Multi-tenant isolation validated
✅ Zero data corruption scenarios
✅ 90%+ code coverage
✅ Automated test execution in CI/CD
✅ Complete documentation and runbooks

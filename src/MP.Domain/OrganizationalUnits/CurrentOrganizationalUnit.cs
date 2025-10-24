using System;
using Microsoft.AspNetCore.Http;
using Volo.Abp;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Implementation of ICurrentOrganizationalUnit using HttpContext.Items for storage
    /// Allows tracking and changing the current organizational unit context
    /// </summary>
    public class CurrentOrganizationalUnit : ICurrentOrganizationalUnit
    {
        private const string HttpContextItemKey = "CurrentOrganizationalUnit";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentOrganizationalUnit(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets the ID of the current organizational unit
        /// </summary>
        /// <exception cref="CurrentOrganizationalUnitNotSetException">Thrown when no organizational unit is set</exception>
        public Guid? Id
        {
            get
            {
                var current = GetCurrentValue();
                return current?.Id;
            }
        }

        /// <summary>
        /// Gets the name of the current organizational unit
        /// </summary>
        /// <exception cref="CurrentOrganizationalUnitNotSetException">Thrown when no organizational unit is set</exception>
        public string? Name
        {
            get
            {
                var current = GetCurrentValue();
                return current?.Name;
            }
        }

        /// <summary>
        /// Indicates whether an organizational unit is currently set
        /// </summary>
        public bool IsAvailable
        {
            get
            {
                var current = GetCurrentValue();
                return current?.Id.HasValue ?? false;
            }
        }

        /// <summary>
        /// Temporarily changes the current organizational unit context
        /// </summary>
        /// <param name="id">The ID of the organizational unit to change to (null to clear)</param>
        /// <param name="name">Optional name of the organizational unit</param>
        /// <returns>An IDisposable that restores the previous organizational unit when disposed</returns>
        public IDisposable Change(Guid? id, string? name = null)
        {
            Check.NotNull(_httpContextAccessor.HttpContext, nameof(_httpContextAccessor.HttpContext));

            var previousValue = GetCurrentValue();
            var newValue = new OrganizationalUnitInfo(id, name);

            SetCurrentValue(newValue);

            return new OrganizationalUnitChange(this, previousValue);
        }

        /// <summary>
        /// Gets the current organizational unit value from HttpContext.Items
        /// </summary>
        private OrganizationalUnitInfo? GetCurrentValue()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            if (httpContext.Items.TryGetValue(HttpContextItemKey, out var value))
            {
                return value as OrganizationalUnitInfo;
            }

            return null;
        }

        /// <summary>
        /// Sets the current organizational unit value in HttpContext.Items
        /// </summary>
        private void SetCurrentValue(OrganizationalUnitInfo? value)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return;
            }

            if (value == null)
            {
                httpContext.Items.Remove(HttpContextItemKey);
            }
            else
            {
                httpContext.Items[HttpContextItemKey] = value;
            }
        }

        /// <summary>
        /// Represents organizational unit information stored in context
        /// </summary>
        private class OrganizationalUnitInfo
        {
            public Guid? Id { get; }
            public string? Name { get; }

            public OrganizationalUnitInfo(Guid? id, string? name = null)
            {
                Id = id;
                Name = name;
            }
        }

        /// <summary>
        /// Disposable wrapper for restoring previous organizational unit context
        /// </summary>
        private class OrganizationalUnitChange : IDisposable
        {
            private readonly CurrentOrganizationalUnit _currentOrganizationalUnit;
            private readonly OrganizationalUnitInfo? _previousValue;
            private bool _disposed;

            public OrganizationalUnitChange(CurrentOrganizationalUnit currentOrganizationalUnit, OrganizationalUnitInfo? previousValue)
            {
                _currentOrganizationalUnit = currentOrganizationalUnit ?? throw new ArgumentNullException(nameof(currentOrganizationalUnit));
                _previousValue = previousValue;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _currentOrganizationalUnit.SetCurrentValue(_previousValue);
            }
        }
    }
}

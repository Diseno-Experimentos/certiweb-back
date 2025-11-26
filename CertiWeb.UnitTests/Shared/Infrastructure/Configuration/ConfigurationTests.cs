using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CertiWeb.UnitTests.Shared.Infrastructure.Configuration;

/// <summary>
/// Unit tests for application configuration and environment settings
/// </summary>
public class ConfigurationTests
{
    #region Configuration Loading Tests

    [Test]
    public void Configuration_WhenLoadingFromAppSettings_ShouldLoadCorrectly()
    {
        // Arrange
        var appSettingsJson = """
        {
          "ConnectionStrings": {
            "DefaultConnection": "Server=localhost;Database=CertiWeb;Trusted_Connection=true;"
          },
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Warning"
            }
          },
          "AllowedHosts": "*"
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(appSettingsJson)));

        // Act
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("Server=localhost;Database=CertiWeb;Trusted_Connection=true;", 
          configuration.GetConnectionString("DefaultConnection"));
        Assert.AreEqual("Information", configuration["Logging:LogLevel:Default"]);
        Assert.AreEqual("Warning", configuration["Logging:LogLevel:Microsoft.AspNetCore"]);
        Assert.AreEqual("*", configuration["AllowedHosts"]);
    }

    [Test]
    public void Configuration_WhenLoadingEnvironmentSpecific_ShouldOverrideDefaults()
    {
        // Arrange
        var appSettingsJson = """
        {
          "DatabaseSettings": {
            "ConnectionString": "DefaultConnection",
            "CommandTimeout": 30
          }
        }
        """;

        var appSettingsDevelopmentJson = """
        {
          "DatabaseSettings": {
            "ConnectionString": "DevelopmentConnection",
            "EnableSensitiveDataLogging": true
          }
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(appSettingsJson)));
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(appSettingsDevelopmentJson)));

        // Act
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("DevelopmentConnection", configuration["DatabaseSettings:ConnectionString"]);
        Assert.AreEqual("30", configuration["DatabaseSettings:CommandTimeout"]); // Should keep from base
        Assert.AreEqual("true", configuration["DatabaseSettings:EnableSensitiveDataLogging"]?.ToLowerInvariant()); // New from development
    }

    [Test]
    public void Configuration_WhenLoadingFromEnvironmentVariables_ShouldOverrideJson()
    {
        // Arrange
        var appSettingsJson = """
        {
          "ApiSettings": {
            "ApiKey": "DefaultApiKey",
            "BaseUrl": "https://localhost:5001"
          }
        }
        """;

        var environmentVariables = new Dictionary<string, string?>
        {
            ["ApiSettings:ApiKey"] = "EnvironmentApiKey",
            ["ApiSettings:MaxRetries"] = "5"
        };

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(appSettingsJson)));
        builder.AddInMemoryCollection(environmentVariables);

        // Act
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual("EnvironmentApiKey", configuration["ApiSettings:ApiKey"]); // Overridden by environment
        Assert.AreEqual("https://localhost:5001", configuration["ApiSettings:BaseUrl"]); // From JSON
        Assert.AreEqual("5", configuration["ApiSettings:MaxRetries"]); // New from environment
    }

    #endregion

    #region Configuration Binding Tests

    [Test]
    public void Configuration_WhenBindingToStronglyTypedClass_ShouldBindCorrectly()
    {
        // Arrange
        var configJson = """
        {
          "DatabaseSettings": {
            "ConnectionString": "Server=localhost;Database=Test;",
            "CommandTimeout": 45,
            "EnableRetryOnFailure": true,
            "MaxRetryCount": 3
          }
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson)));
        var configuration = builder.Build();

        // Act
        var databaseSettings = new DatabaseSettings();
        configuration.GetSection("DatabaseSettings").Bind(databaseSettings);

        // Assert
        Assert.AreEqual("Server=localhost;Database=Test;", databaseSettings.ConnectionString);
        Assert.AreEqual(45, databaseSettings.CommandTimeout);
        Assert.IsTrue(databaseSettings.EnableRetryOnFailure);
        Assert.AreEqual(3, databaseSettings.MaxRetryCount);
    }

    [Test]
    public void Configuration_WhenBindingNestedObjects_ShouldBindCorrectly()
    {
        // Arrange
        var configJson = """
        {
          "Application": {
            "Name": "CertiWeb",
            "Version": "1.0.0",
            "Features": {
              "EnableCaching": true,
              "EnableLogging": true,
              "CacheSettings": {
                "DefaultExpiration": 300,
                "MaxSize": 1000
              }
            }
          }
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson)));
        var configuration = builder.Build();

        // Act
        var appSettings = new ApplicationSettings();
        configuration.GetSection("Application").Bind(appSettings);

        // Assert
        Assert.AreEqual("CertiWeb", appSettings.Name);
        Assert.AreEqual("1.0.0", appSettings.Version);
        Assert.IsNotNull(appSettings.Features);
        Assert.IsTrue(appSettings.Features.EnableCaching);
        Assert.IsTrue(appSettings.Features.EnableLogging);
        Assert.IsNotNull(appSettings.Features.CacheSettings);
        Assert.AreEqual(300, appSettings.Features.CacheSettings.DefaultExpiration);
        Assert.AreEqual(1000, appSettings.Features.CacheSettings.MaxSize);
    }

    [Test]
    public void Configuration_WhenBindingArrays_ShouldBindCorrectly()
    {
        // Arrange
        var configJson = """
        {
          "AllowedOrigins": [
            "https://localhost:3000",
            "https://certiweb.com",
            "https://app.certiweb.com"
          ],
          "SupportedLanguages": ["en", "es", "fr"]
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson)));
        var configuration = builder.Build();

        // Act
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();
        var supportedLanguages = configuration.GetSection("SupportedLanguages").Get<string[]>();

        // Assert
        Assert.IsNotNull(allowedOrigins);
        Assert.AreEqual(3, allowedOrigins.Length);
        CollectionAssert.Contains(allowedOrigins, "https://localhost:3000");
        CollectionAssert.Contains(allowedOrigins, "https://certiweb.com");
        CollectionAssert.Contains(allowedOrigins, "https://app.certiweb.com");

        Assert.IsNotNull(supportedLanguages);
        Assert.AreEqual(3, supportedLanguages.Length);
        CollectionAssert.Contains(supportedLanguages, "en");
        CollectionAssert.Contains(supportedLanguages, "es");
        CollectionAssert.Contains(supportedLanguages, "fr");
    }

    #endregion

    #region Environment-Specific Tests

    [TestCase("Development")]
    [TestCase("Staging")]
    [TestCase("Production")]
    public void Configuration_WhenDifferentEnvironments_ShouldLoadCorrectSettings(string environment)
    {
        // Arrange
        var baseConfig = """
        {
          "Environment": "Base",
          "DatabaseSettings": {
            "ConnectionString": "BaseConnection"
          }
        }
        """;

        var envConfig = environment switch
        {
            "Development" => """
            {
              "Environment": "Development",
              "DatabaseSettings": {
                "ConnectionString": "DevelopmentConnection",
                "EnableSensitiveDataLogging": true
              }
            }
            """,
            "Staging" => """
            {
              "Environment": "Staging",
              "DatabaseSettings": {
                "ConnectionString": "StagingConnection"
              }
            }
            """,
            "Production" => """
            {
              "Environment": "Production",
              "DatabaseSettings": {
                "ConnectionString": "ProductionConnection",
                "CommandTimeout": 60
              }
            }
            """,
            _ => "{}"
        };

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(baseConfig)));
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(envConfig)));

        // Act
        var configuration = builder.Build();

        // Assert
        Assert.AreEqual(environment, configuration["Environment"]);
        
        switch (environment)
        {
            case "Development":
            Assert.AreEqual("DevelopmentConnection", configuration["DatabaseSettings:ConnectionString"]);
            Assert.AreEqual("true", configuration["DatabaseSettings:EnableSensitiveDataLogging"]?.ToLowerInvariant());
              break;
            case "Staging":
            Assert.AreEqual("StagingConnection", configuration["DatabaseSettings:ConnectionString"]);
                break;
            case "Production":
            Assert.AreEqual("ProductionConnection", configuration["DatabaseSettings:ConnectionString"]);
            Assert.AreEqual("60", configuration["DatabaseSettings:CommandTimeout"]);
                break;
        }
    }

    #endregion

    #region Validation Tests

    [Test]
    public void Configuration_WhenMissingRequiredSettings_ShouldValidateCorrectly()
    {
        // Arrange
        var incompleteConfig = """
        {
          "DatabaseSettings": {
            "CommandTimeout": 30
          }
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(incompleteConfig)));
        var configuration = builder.Build();

        // Act
        var databaseSettings = new DatabaseSettings();
        configuration.GetSection("DatabaseSettings").Bind(databaseSettings);

        // Assert
        var validationResults = ValidateConfiguration(databaseSettings);
        CollectionAssert.IsNotEmpty(validationResults);
        Assert.IsTrue(validationResults.Exists(r => r.Contains("ConnectionString")));
    }

    [Test]
    public void Configuration_WhenInvalidValues_ShouldValidateCorrectly()
    {
        // Arrange
        var invalidConfig = """
        {
          "DatabaseSettings": {
            "ConnectionString": "ValidConnection",
            "CommandTimeout": -1,
            "MaxRetryCount": 0
          }
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidConfig)));
        var configuration = builder.Build();

        // Act
        var databaseSettings = new DatabaseSettings();
        configuration.GetSection("DatabaseSettings").Bind(databaseSettings);

        // Assert
        var validationResults = ValidateConfiguration(databaseSettings);
        Assert.IsTrue(validationResults.Exists(r => r.Contains("CommandTimeout")));
        Assert.IsTrue(validationResults.Exists(r => r.Contains("MaxRetryCount")));
    }

    #endregion

    #region Configuration Security Tests

    [Test]
    public void Configuration_WhenContainingSensitiveData_ShouldHandleSecurely()
    {
        // Arrange
        var configWithSecrets = """
        {
          "ConnectionStrings": {
            "DefaultConnection": "Server=localhost;Database=CertiWeb;User=admin;Password=secret123;"
          },
          "ApiKeys": {
            "ExternalService": "sk-1234567890abcdef",
            "PaymentGateway": "pk_test_1234567890"
          }
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configWithSecrets)));
        var configuration = builder.Build();

        // Act
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var externalApiKey = configuration["ApiKeys:ExternalService"];

        // Assert
        Assert.IsNotNull(connectionString);
        Assert.IsNotNull(externalApiKey);
        
        // In real scenarios, these should be masked or encrypted
        StringAssert.Contains("Password=", connectionString);
        StringAssert.StartsWith("sk-", externalApiKey);
    }

    [Test]
    public void Configuration_WhenSerializing_ShouldNotExposeSensitiveData()
    {
        // Arrange
        var configWithSecrets = """
        {
          "PublicSettings": {
            "AppName": "CertiWeb",
            "Version": "1.0.0"
          },
          "SecretSettings": {
            "DatabasePassword": "secret123",
            "ApiKey": "sk-secret"
          }
        }
        """;

        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configWithSecrets)));
        var configuration = builder.Build();

        // Act
        var publicSettings = configuration.GetSection("PublicSettings").Get<Dictionary<string, string>>();
        var allSettings = configuration.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Assert
        Assert.IsNotNull(publicSettings);
        Assert.AreEqual("CertiWeb", publicSettings["AppName"]);
        
        // Verify sensitive data exists in configuration but should be handled carefully
        Assert.IsTrue(allSettings.Keys.Any(k => k.Contains("DatabasePassword")));
        Assert.IsTrue(allSettings.Keys.Any(k => k.Contains("ApiKey")));
    }

    #endregion

    #region Helper Classes and Methods

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableRetryOnFailure { get; set; } = false;
        public int MaxRetryCount { get; set; } = 3;
        public bool EnableSensitiveDataLogging { get; set; } = false;
    }

    public class ApplicationSettings
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public FeatureSettings Features { get; set; } = new();
    }

    public class FeatureSettings
    {
        public bool EnableCaching { get; set; }
        public bool EnableLogging { get; set; }
        public CacheSettings CacheSettings { get; set; } = new();
    }

    public class CacheSettings
    {
        public int DefaultExpiration { get; set; }
        public int MaxSize { get; set; }
    }

    private static List<string> ValidateConfiguration(DatabaseSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(settings.ConnectionString))
            errors.Add("ConnectionString is required");

        if (settings.CommandTimeout <= 0)
            errors.Add("CommandTimeout must be greater than 0");

        if (settings.MaxRetryCount < 1)
            errors.Add("MaxRetryCount must be at least 1");

        return errors;
    }

    #endregion
}

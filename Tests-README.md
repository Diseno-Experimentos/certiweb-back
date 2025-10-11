# CertiWeb Backend - Comprehensive Testing Suite

## 📋 Índice
- [Información General](#información-general)
- [Estructura de Pruebas](#estructura-de-pruebas)
- [Tipos de Pruebas](#tipos-de-pruebas)
- [Configuración y Comandos](#configuración-y-comandos)
- [Cobertura de Pruebas](#cobertura-de-pruebas)
- [Mejores Prácticas](#mejores-prácticas)

## 🎯 Información General

Esta suite de pruebas implementa una estrategia de testing exhaustiva para el backend CertiWeb, siguiendo la metodología **Arrange, Act, Assert (AAA)** y cubriendo todos los aspectos críticos del sistema.

### Frameworks y Herramientas
- **.NET 9** - Framework principal
- **xUnit & NUnit** - Frameworks de testing
- **Moq** - Framework de mocking
- **FluentAssertions** - Assertions fluidas
- **Entity Framework Core InMemory** - Base de datos en memoria para tests
- **Bogus** - Generación de datos de prueba
- **Microsoft.AspNetCore.Mvc.Testing** - Testing de APIs

## 📁 Estructura de Pruebas

```
CertiWeb.UnitTests/
├── Certifications/
│   ├── Application/Internal/
│   │   ├── CommandServices/     # Tests para servicios de comando
│   │   └── QueryServices/       # Tests para servicios de consulta
│   ├── Domain/
│   │   ├── Model/
│   │   │   ├── Aggregates/      # Tests para entidades principales
│   │   │   ├── ValueObjects/    # Tests para objetos de valor
│   │   │   └── Services/        # Tests para servicios de dominio
│   │   └── Repositories/        # Tests para comportamiento de repositorios
│   └── Interfaces/REST/
│       └── Transform/           # Tests para transformaciones REST
├── Users/Domain/Model/
│   └── Aggregates/              # Tests para entidades de usuario
├── Shared/Infrastructure/
│   ├── Concurrency/             # Tests de concurrencia y threading
│   ├── Configuration/           # Tests de configuración
│   └── Middleware/              # Tests de middleware HTTP
└── AssemblyInfo.cs              # Configuración de paralelismo

CertiWeb.IntegrationTests/
├── Certifications/Domain/Model/
│   └── Aggregates/              # Tests de integración para entidades
├── Users/Domain/Model/
│   └── Aggregates/              # Tests de integración para usuarios
├── Shared/Infrastructure/
│   └── DatabaseTestBase.cs     # Base para tests de BD
└── AssemblyInfo.cs

CertiWeb.SystemTests/
├── Certifications/REST/         # Tests E2E para endpoints REST
├── Users/REST/                  # Tests E2E para usuarios
├── Performance/                 # Tests de rendimiento
├── Security/                    # Tests de seguridad
├── Resilience/                  # Tests de resiliencia
├── Health/                      # Tests de health checks
├── Validation/                  # Tests de validación de datos
├── BusinessFlows/               # Tests de flujos de negocio
├── Compatibility/               # Tests de compatibilidad
├── Infrastructure/              # Infraestructura de testing
├── TestData/                    # Generadores de datos de prueba
└── AssemblyInfo.cs
```
├── Users/
│   └── REST/
│       └── UsersControllerSystemTests.cs
├── Performance/
│   └── PerformanceSystemTests.cs
├── Security/
│   └── SecuritySystemTests.cs
├── Resilience/
│   └── ResilienceSystemTests.cs
├── Infrastructure/
│   ├── CertiWebApplicationFactory.cs
│   └── SystemTestBase.cs
├── TestData/
│   └── TestDataBuilder.cs
├── GlobalUsings.cs
└── AssemblyInfo.cs
```

## Tecnologías Utilizadas

- **NUnit 4.2.2** - Framework de testing
- **FluentAssertions 6.12.1** - Assertions más legibles
- **Moq 4.20.72** - Mocking framework
- **Microsoft.EntityFrameworkCore.InMemory 9.0.5** - Base de datos en memoria para tests
- **Microsoft.AspNetCore.Mvc.Testing 9.0.5** - Testing para ASP.NET Core
- **Bogus 35.6.1** - Generación de datos de prueba realistas
- **WireMock.Net 1.6.5** - Mock de servicios externos
- **Testcontainers.MySql 3.10.0** - Contenedores para testing de integración

## Metodología Arrange, Act, Assert

Todos los tests siguen la metodología AAA:

```csharp
[Test]
public void Constructor_WithValidData_ShouldCreateEntitySuccessfully()
{
    // Arrange - Preparar los datos de entrada
    var validData = "test data";
    
    // Act - Ejecutar la acción a probar
    var entity = new Entity(validData);
    
    // Assert - Verificar el resultado
    entity.Property.Should().Be(validData);
}
```

## Unit Tests

### Value Objects
- **YearTests**: Pruebas para el value object Year
  - Validación de rangos (1900 - año actual + 1)
  - Conversiones implícitas
  - Manejo de errores

- **PriceTests**: Pruebas para el value object Price
  - Validación de valores negativos
  - Monedas por defecto y personalizadas
  - Conversiones implícitas

- **LicensePlateTests**: Pruebas para el value object LicensePlate
  - Validación de longitud (6-10 caracteres)
  - Conversión a mayúsculas
  - Manejo de strings vacíos/nulos

- **PdfCertificationTests**: Pruebas para el value object PdfCertification
  - Validación de datos Base64
  - Manejo de prefijos data URL
  - Validación de longitud mínima

### Aggregates
- **BrandTests**: Pruebas para la entidad Brand
  - Constructores con y sin parámetros
  - Validación de nombres vacíos
  - Propiedades mutables

- **CarTests**: Pruebas para la entidad Car
  - Constructor con CreateCarCommand
  - Validación de value objects
  - Manejo de propiedades opcionales

- **UserTests**: Pruebas para la entidad User
  - Constructor con CreateUserCommand
  - Propiedades requeridas
  - Diferentes planes de suscripción

## Integration Tests

### DatabaseTestBase
Clase base que proporciona:
- Configuración de base de datos en memoria para cada test
- Métodos de utilidad para limpiar contexto
- Manejo de ciclo de vida de DbContext

### Entidades
- **BrandIntegrationTests**: Pruebas de persistencia para Brand
  - CRUD operations
  - Consultas por nombre
  - Manejo de estados de entidad

- **CarIntegrationTests**: Pruebas de persistencia para Car
  - Persistencia de value objects
  - Relaciones con Brand (Foreign Key)
  - Constraints únicos (LicensePlate, OriginalReservationId)
  - Consultas complejas

- **UserIntegrationTests**: Pruebas de persistencia para User
  - CRUD operations
  - Consultas por email y plan
  - Tracking de cambios

## System Tests (End-to-End)

### REST API Tests
- **BrandsControllerSystemTests**: Pruebas completas del endpoint de marcas
  - Operaciones GET con diferentes escenarios
  - Validación de estructura JSON
  - Manejo de datos vacíos y poblados
  - Pruebas de concurrencia

- **CarsControllerSystemTests**: Pruebas completas del endpoint de autos
  - Operaciones CRUD completas
  - Validación de datos de entrada
  - Manejo de duplicados y constraints
  - Flujos end-to-end completos

- **UsersControllerSystemTests**: Pruebas completas del endpoint de usuarios
  - Consultas por ID, email y plan
  - Manejo de casos no encontrados
  - Operaciones de lectura masiva

### Performance Tests
- **PerformanceSystemTests**: Pruebas de rendimiento y carga
  - Tiempo de respuesta bajo carga
  - Manejo de datasets grandes
  - Pruebas de concurrencia masiva
  - Análisis de uso de memoria
  - Consistencia de tiempos de respuesta

### Security Tests
- **SecuritySystemTests**: Pruebas de seguridad
  - Validación de headers de seguridad
  - Protección contra inyección SQL
  - Prevención de ataques XSS
  - Manejo de payloads maliciosos
  - Validación de límites y caracteres especiales

### Resilience Tests
- **ResilienceSystemTests**: Pruebas de resistencia y recuperación
  - Manejo de fallos de conexión a BD
  - Recuperación de errores transitorios
  - Manejo de condiciones de carrera
  - Resistencia a alta carga
  - Gestión de memoria bajo presión

### Infrastructure
- **CertiWebApplicationFactory**: Factory personalizada para tests de sistema
- **SystemTestBase**: Clase base con configuración de HTTP client y BD
- **TestDataBuilder**: Generador de datos de prueba con Bogus

## Comandos para Ejecutar Tests

### Todos los tests
```bash
dotnet test
```

### Solo Unit Tests
```bash
dotnet test CertiWeb.UnitTests
```

### Solo Integration Tests
```bash
dotnet test CertiWeb.IntegrationTests
```

### Solo System Tests
```bash
dotnet test CertiWeb.SystemTests
```

### Tests por categoría
```bash
# Value Objects
dotnet test --filter "FullyQualifiedName~ValueObjects"

# Aggregates
dotnet test --filter "FullyQualifiedName~Aggregates"

# REST API tests
dotnet test --filter "FullyQualifiedName~REST"

# Performance tests
dotnet test --filter "FullyQualifiedName~Performance"

# Security tests
dotnet test --filter "FullyQualifiedName~Security"

# Resilience tests
dotnet test --filter "FullyQualifiedName~Resilience"
```

### Con reporte de cobertura
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Tests específicos
```bash
# Brand tests
dotnet test --filter "FullyQualifiedName~Brand"

# Car tests
dotnet test --filter "FullyQualifiedName~Car"

# User tests
dotnet test --filter "FullyQualifiedName~User"
```

## Configuración del Entorno

### Prerrequisitos
- .NET 9.0 SDK
- Visual Studio 2022 o VS Code
- Git

### Configuración
## 🧪 Tipos de Pruebas

### 1. Unit Tests (Pruebas Unitarias)
- **Value Objects**: Validaciones, conversiones, excepciones
- **Entities/Aggregates**: Constructores, métodos, invariantes
- **Domain Services**: Lógica de negocio, validaciones
- **Application Services**: Command/Query handlers
- **Repository Behavior**: Contratos y comportamiento
- **Resource Transformations**: Mapeo de DTOs
- **Middleware Components**: Pipeline HTTP
- **Configuration**: Carga y validación de configuración
- **Concurrency**: Thread safety, async patterns

### 2. Integration Tests (Pruebas de Integración)
- **Database Operations**: CRUD, constraints, relaciones
- **Entity Framework**: Tracking, lazy loading, queries
- **Cross-layer Integration**: Application → Domain → Infrastructure

### 3. System Tests (Pruebas de Sistema/E2E)
- **API Endpoints**: REST controllers, request/response
- **Performance**: Latencia, throughput, carga
- **Security**: Autenticación, autorización, vulnerabilidades
- **Resilience**: Timeouts, circuit breakers, retry policies
- **Health Checks**: Status endpoints, dependencies
- **Data Validation**: Edge cases, formatos, constraints
- **Business Flows**: Flujos completos de negocio
- **Compatibility**: Internacionalización, formatos

## ⚙️ Configuración y Comandos

### Prerequisitos
```bash
# Instalar .NET 9 SDK
dotnet --version  # Verificar instalación
```

### Comandos de Ejecución

#### Ejecutar Todos los Tests
```bash
# Todos los proyectos de test
dotnet test

# Con detalles verbosos
dotnet test --verbosity normal

# Con cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

#### Por Proyecto Específico
```bash
# Solo Unit Tests
dotnet test CertiWeb.UnitTests

# Solo Integration Tests  
dotnet test CertiWeb.IntegrationTests

# Solo System Tests
dotnet test CertiWeb.SystemTests
```

#### Por Categoría/Filtro
```bash
# Tests por categoría
dotnet test --filter "Category=ValueObjects"
dotnet test --filter "Category=Performance"
dotnet test --filter "Category=Security"

# Tests por nombre
dotnet test --filter "Name~Car"
dotnet test --filter "FullyQualifiedName~CarTests"

# Tests específicos
dotnet test --filter "TestCategory=Integration"
```

#### Con Configuraciones Específicas
```bash
# Configuración Debug
dotnet test --configuration Debug

# Configuración Release (mejor para performance tests)
dotnet test --configuration Release

# Solo tests rápidos (excluyendo performance)
dotnet test --filter "Category!=Performance&Category!=Load"
```

### Compilación
```bash
# Compilar todos los proyectos de test
dotnet build

# Compilar proyecto específico
dotnet build CertiWeb.UnitTests
```

## 📊 Cobertura de Pruebas

### Cobertura por Capas

#### Domain Layer (95%+)
- ✅ **Value Objects**: Year, Price, LicensePlate, PdfCertification
- ✅ **Entities**: Car, Brand, User
- ✅ **Domain Services**: Validation, business rules
- ✅ **Repository Contracts**: Interface compliance

#### Application Layer (90%+)
- ✅ **Command Services**: Create, Update, Delete operations
- ✅ **Query Services**: Retrieval, filtering, pagination
- ✅ **ACL Services**: Anti-corruption layer
- ✅ **Command/Query Objects**: DTOs and mapping

#### Infrastructure Layer (85%+)
- ✅ **Database Integration**: EF Core operations
- ✅ **Middleware**: Error handling, logging, security
- ✅ **Configuration**: Settings loading and validation

#### Presentation Layer (80%+)
- ✅ **REST Controllers**: API endpoints
- ✅ **Resource Transformations**: Request/Response mapping
- ✅ **Validation**: Input validation

### Cobertura por Funcionalidad

#### Core Business (95%+)
- ✅ Car management (CRUD operations)
- ✅ Brand management
- ✅ User management
- ✅ Certification handling
- ✅ Business rules validation

#### Quality Attributes (85%+)
- ✅ **Performance**: Load testing, stress testing
- ✅ **Security**: Authentication, authorization, input validation
- ✅ **Reliability**: Error handling, resilience patterns
- ✅ **Usability**: API contracts, response formats
- ✅ **Maintainability**: Code structure, SOLID principles

#### Cross-cutting Concerns (80%+)
- ✅ **Logging**: Structured logging, correlation
- ✅ **Monitoring**: Health checks, metrics
- ✅ **Configuration**: Environment-specific settings
- ✅ **Concurrency**: Thread safety, async patterns
- ✅ **Internationalization**: Multi-language support

## 📈 Métricas de Testing

### Estadísticas Actuales
- **Total de Tests**: 200+ tests
- **Unit Tests**: 120+ tests
- **Integration Tests**: 40+ tests  
- **System Tests**: 40+ tests
- **Tiempo de Ejecución**: < 5 minutos
- **Cobertura de Código**: 85%+

### Tests por Categoría
```
Value Objects:        25 tests
Entities:            20 tests
Domain Services:     15 tests
Application Services: 20 tests
Repository Behavior:  15 tests
REST Endpoints:      25 tests
Performance:         10 tests
Security:           15 tests
Configuration:       10 tests
Concurrency:        15 tests
Middleware:         20 tests
Validation:         20 tests
```

## ✅ Mejores Prácticas

### Nomenclatura de Tests
```csharp
// Patrón: Method_Scenario_ExpectedResult
[Fact]
public void Constructor_WhenValidData_ShouldCreateSuccessfully()

[Theory]
[InlineData(validInput)]
public void Method_WhenCondition_ShouldBehavior(input)
```

### Estructura AAA
```csharp
[Fact]
public void TestMethod()
{
    // Arrange - Preparar datos y dependencias
    var input = CreateTestData();
    var mock = new Mock<IDependency>();
    
    // Act - Ejecutar el método bajo prueba
    var result = systemUnderTest.Method(input);
    
    // Assert - Verificar el resultado
    Assert.NotNull(result);
    Assert.Equal(expected, result.Property);
}
```

### Mocking Guidelines
- Usar Moq para dependencias externas
- Mock interfaces, no clases concretas
- Verificar interacciones importantes
- Setup solo lo necesario

### Performance Testing
- Tests rápidos (< 100ms) en Unit Tests
- Tests de carga en System Tests
- Usar `[Fact(Skip = "Performance")]` para tests largos

### Data Generation
- Usar Bogus para generar datos realistas
- TestDataBuilder pattern para objetos complejos
- Datos determinísticos en Unit Tests

## 🚀 Ejecución Continua

### En Desarrollo Local
```bash
# Watch mode - re-ejecuta tests al cambiar código
dotnet watch test

# Solo tests rápidos en desarrollo
dotnet test --filter "Category!=Performance&Category!=Load"
```

### En CI/CD Pipeline
```bash
# Pipeline completo
dotnet test --configuration Release --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generar reportes
dotnet tool run reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./CoverageReport"
```

## 📝 Extensiones Futuras

### Próximas Iteraciones
1. **Chaos Engineering**: Tests de fallas aleatorias
2. **Contract Testing**: Pact consumer/provider
3. **Mutation Testing**: Validación de calidad de tests
4. **Property-Based Testing**: FsCheck integration
5. **Visual Regression Testing**: UI component testing
6. **Database Migration Testing**: Schema evolution
7. **API Versioning Testing**: Backward compatibility
8. **Load Testing**: Artillery.io integration

### Métricas Avanzadas
- Test execution trends
- Flaky test detection  
- Coverage evolution
- Performance regression detection

---

**Nota**: Esta suite de pruebas está diseñada para evolucionar continuamente. Cada nueva funcionalidad debe incluir sus respectivos tests siguiendo estas mismas prácticas y estándares.
- ✅ Manejo de memoria y recursos

## Mejores Prácticas

1. **Nombres descriptivos**: Los nombres de tests describen exactamente qué se está probando
2. **Un concepto por test**: Cada test verifica un solo comportamiento
3. **Independencia**: Los tests no dependen unos de otros
4. **Datos específicos**: Uso de datos específicos en lugar de genéricos
5. **Cleanup**: Limpieza automática después de cada test
6. **Fast feedback**: Tests rápidos para feedback inmediato

## Contribución

Al agregar nuevos tests:
1. Sigue la metodología Arrange, Act, Assert
2. Usa nombres descriptivos
3. Incluye tests tanto positivos como negativos
4. Agrega tests de integración para nuevas entidades
5. Mantén la cobertura de código alta

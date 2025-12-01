# CertiWeb Backend

Backend API para CertiWeb, una plataforma de certificación de vehículos construida con .NET 9.

## Requisitos previos

- .NET 9 SDK
- PostgreSQL (para desarrollo local)
- Docker (opcional, para contenedores)

## Estructura del proyecto

```
certiweb-back/
├── CertiWeb.API/              # API principal
├── CertiWeb.UnitTests/        # Tests unitarios
├── CertiWeb.IntegrationTests/ # Tests de integración
├── CertiWeb.SystemTests/      # Tests de sistema/E2E
└── TestResults/               # Resultados de tests y cobertura
```

## Configuración

1. Clonar el repositorio
2. Configurar la cadena de conexión en `appsettings.Development.json`
3. Ejecutar las migraciones de base de datos

## Ejecución de tests

### Ejecutar todos los tests

```bash
dotnet test
```

### Ejecutar tests por proyecto

```bash
# Tests unitarios
dotnet test CertiWeb.UnitTests/CertiWeb.UnitTests.csproj

# Tests de integración
dotnet test CertiWeb.IntegrationTests/CertiWeb.IntegrationTests.csproj

# Tests de sistema
dotnet test CertiWeb.SystemTests/CertiWeb.SystemTests.csproj
```

### Ejecutar tests con filtros

```bash
# Ejecutar solo tests de una clase específica
dotnet test --filter "FullyQualifiedName~CarCommandServiceTests"

# Ejecutar solo tests que contengan "Create" en el nombre
dotnet test --filter "Name~Create"

# Ejecutar tests por categoría (si están definidas)
dotnet test --filter "Category=Unit"
```

### Ejecutar tests en modo watch

```bash
dotnet watch test --project CertiWeb.UnitTests/CertiWeb.UnitTests.csproj
```

## Cobertura de código

### Ejecutar tests con cobertura

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings
```

Este comando ejecutará todos los tests y generará archivos de cobertura en formato Cobertura XML en la carpeta `TestResults` de cada proyecto de test.

### Generar reporte HTML de cobertura

1. Instalar ReportGenerator (solo una vez):

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

2. Generar el reporte:

```bash
reportgenerator -reports:"**\coverage.cobertura.xml" -targetdir:"TestResults\CoverageReport" -reporttypes:"Html;Cobertura"
```

3. Abrir el reporte:

```bash
# Windows
start TestResults\CoverageReport\index.html

# Linux/macOS
xdg-open TestResults/CoverageReport/index.html
```

### Interpretar el reporte de cobertura

El reporte HTML muestra:

- **Line Coverage**: Porcentaje de líneas de código ejecutadas por los tests
- **Branch Coverage**: Porcentaje de ramas condicionales (if, switch, etc.) ejecutadas
- **Covered/Uncovered lines**: Número absoluto de líneas cubiertas y no cubiertas
- **Risk Hotspots**: Código complejo con baja cobertura que requiere atención

Colores en el reporte:
- Verde: Buena cobertura (>80%)
- Amarillo: Cobertura media (40-80%)
- Rojo: Baja cobertura (<40%)

### Generar reporte en CI/CD

El proyecto está configurado para generar reportes de cobertura automáticamente en el pipeline de CI/CD. Los reportes se guardan como artefactos y pueden descargarse desde GitHub Actions.

### Mejorar la cobertura

Para mejorar la cobertura de código:

1. Identificar clases con baja cobertura en el reporte HTML
2. Revisar las líneas no cubiertas (marcadas en rojo)
3. Escribir tests que ejerciten esos caminos de código
4. Ejecutar tests con cobertura nuevamente para verificar mejoras

Áreas prioritarias para tests:
- Lógica de negocio en servicios de comandos y consultas
- Validaciones en agregados de dominio
- Transformaciones en assemblers
- Manejo de errores en controladores

## Convenciones de testing

### Nomenclatura de tests

Utilizar el patrón: `MethodName_Scenario_ExpectedBehavior`

Ejemplos:
```csharp
CreateCar_WithValidData_ShouldReturnCar()
CreateCar_WithInvalidLicensePlate_ShouldThrowException()
GetCarById_WhenCarDoesNotExist_ShouldReturnNull()
```

### Estructura de tests (AAA)

```csharp
[Test]
public async Task TestName()
{
    // Arrange - Preparar datos de prueba
    var command = new CreateCarCommand(...);
    
    // Act - Ejecutar la acción
    var result = await _service.Handle(command);
    
    // Assert - Verificar resultados
    result.Should().NotBeNull();
    result.LicensePlate.Should().Be("ABC-123");
}
```

### Uso de FluentAssertions

El proyecto utiliza FluentAssertions para aserciones más legibles:

```csharp
// En lugar de: Assert.AreEqual(expected, actual)
result.Should().Be(expected);

// En lugar de: Assert.IsNull(result)
result.Should().BeNull();

// En lugar de: Assert.Throws<Exception>(() => method())
FluentActions.Invoking(() => method()).Should().Throw<Exception>();
```

## Comandos útiles

### Limpiar resultados de tests

```bash
# Limpiar artifacts de test
dotnet clean

# Eliminar carpetas TestResults
rm -rf **/TestResults
```

### Ejecutar tests en paralelo

```bash
dotnet test --parallel
```

### Ver output detallado

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Generar reporte TRX

```bash
dotnet test --logger "trx;LogFileName=test-results.trx"
```

## Troubleshooting

### Tests fallan en CI pero pasan localmente

- Verificar diferencias de zona horaria
- Asegurar que la base de datos de test esté limpia
- Revisar dependencias de timing (usar Task.Delay con precaución)

### Cobertura no refleja cambios recientes

- Limpiar artifacts: `dotnet clean`
- Eliminar carpetas TestResults
- Ejecutar tests con cobertura nuevamente

### ReportGenerator no genera el reporte

- Verificar que los archivos coverage.cobertura.xml existan
- Asegurar que la ruta en el comando -reports sea correcta
- Verificar la instalación: `dotnet tool list -g`

## Contribuir

1. Crear una rama feature/bugfix
2. Escribir tests para nuevas funcionalidades
3. Asegurar que todos los tests pasen
4. Verificar que la cobertura no disminuya
5. Crear Pull Request

## Recursos adicionales

- [NUnit Documentation](https://docs.nunit.org/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [.NET Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [ReportGenerator Documentation](https://reportgenerator.io/)

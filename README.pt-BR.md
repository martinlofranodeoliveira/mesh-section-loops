# mesh-section-loops

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-blue.svg)](https://learn.microsoft.com/dotnet/csharp/)
[![Exact Arithmetic](https://img.shields.io/badge/Matem%C3%A1tica-Racional%20Exata-brightgreen.svg)](#aritmética-racional-exata)
[![Zero NuGet Runtime Deps](https://img.shields.io/badge/Depend%C3%AAncias-Zero%20NuGet-success.svg)](#zero-dependências)

> **Um SDK exato de seccionamento de malhas por planos em C# (.NET 10) para superfícies triangulares orientadas. Produz contornos poligonais fechados que preservam a topologia com coordenadas racionais exatas em BigInteger, predicados inteiros e classificação explícita de degenerescência.**

---

## Visão Geral

O fatiamento planar (*slicing*) é uma operação fundamental em geometria computacional, pré-processamento de elementos finitos e manufatura aditiva (fatiadores de impressão 3D). Fatiadores tradicionais dependem da aritmética de ponto flutuante IEEE 754 e de heurísticas de tolerância de soldagem (*welding tolerance*, como $\epsilon = 10^{-5}$) para conectar segmentos em contornos fechados. Em casos de borda ou cortes quase tangentes, erros de arredondamento criam inevitavelmente microfissuras, autointerseções ou topologias espúrias.

O `mesh-section-loops` elimina aproximações de ponto flutuante resolvendo a interseção malha-plano de forma **exata**:
- **Predicados Inteiros**: Avalia distâncias com sinal vértice-plano $h(v) = \mathbf{n} \cdot \mathbf{v} - d$ rigorosamente contidas em um envelope inteiro de 38 bits.
- **Coordenadas Racionais Exatas**: Calcula pontos de interseção $\mathbf{p} = \frac{u \cdot (-h(v)) + v \cdot h(u)}{h(u) - h(v)}$ como frações canônicas irredutíveis utilizando `System.Numerics.BigInteger`.
- **Montagem Topológica de Contornos**: Une contornos fechados diretamente pelas incidências de meio-arestas (*half-edges*) dos triângulos, garantindo contornos topologicamente fechados sem buscas por proximidade espacial.
- **Classificação Explícita de Degenerescências**: Classifica planos que passam por vértices da malha ($h(v) = 0$) como `DegenerateCut`, sem mascará-los como aproximações lineares.
- **Simplificação Colinear**: Remove pontos de grau 2 redundantes ao longo de arestas planares através do produto vetorial racional exato em 3D.

---

## Recursos Principais

1. **Validador de 2-Variedades Fechadas (2-Manifold)**:
   - Garante emparelhamento de orientação: para cada meio-aresta dirigida $(u, v)$, deve existir exatamente uma meio-aresta oposta $(v, u)$.
   - Valida o ciclo de guarda-chuva (*umbrella cycle*) de cada vértice, rejeitando cones não-variedade pinçados.
   - Rejeita triângulos com área geométrica nula.
2. **Geometria Racional Exata (`Rational`)**:
   - Zero perda de precisão em todas as operações aritméticas.
   - Coordenadas tridimensionais exatas `RationalPoint3D` e projeções bidimensionais canônicas `RationalPoint2D`.
   - Área exata de polígonos pela fórmula de Gauss (*shoelace formula*).
3. **Classificação Estruturada de Resultados**:
   - `CompleteLoops`: Contornos fechados não vazios produzidos com sucesso.
   - `CompleteEmpty`: Plano disjunto da malha.
   - `DegenerateCut`: Plano intercepta um ou mais vértices ($h(v) = 0$).
   - `InvalidMesh`: Topologia não-variedade ou triângulos degenerados detectados.
   - `InvalidPlane`: Normal nula ou parâmetros fora dos limites.
   - `Incomplete`: Orçamento de passos de travessia esgotado.

---

## Limites e Invariantes

| Métrica | Limite | Descrição |
|:---:|:---:|:---|
| Máximo de Vértices | 16.384 | Cardinalidade de vértices ($V \le 2^{14}$) |
| Máximo de Triângulos | 32.768 | Cardinalidade de triângulos ($F \le 2^{15}$) |
| Máximo de Incidências | 98.304 | Incidências de meio-arestas dirigidas ($3 \times 32768$) |
| Limites das Coordenadas | $[-524288, 524287]$ | Coordenadas inteiras de 20 bits com sinal |
| Vetor Normal do Plano | $[-32768, 32767]$ | Inteiros de 16 bits com $\mathbf{n} \ne \mathbf{0}$ |
| Deslocamento $D$ do Plano | $[-2^{36}, 2^{36}-1]$ | Inteiro de 37 bits com sinal |
| Envelope da Distância $h(v)$ | $[-2^{37}, 2^{37}-1]$ | Envelope inteiro com sinal de 38 bits |

---

## Início Rápido e Uso da CLI

### Compilação e Execução de Testes
```bash
dotnet build MeshSectionLoops.sln
dotnet run --project tests/MeshSectionLoops.Tests/MeshSectionLoops.Tests.csproj
```

### Executar Demonstrações com Validação
```bash
dotnet run --project src/MeshSectionLoops.Cli/MeshSectionLoops.Cli.csproj -- demo --assert
```

### Emitir Diagnósticos em Formato JSON
```bash
dotnet run --project src/MeshSectionLoops.Cli/MeshSectionLoops.Cli.csproj -- demo --json
```

---

## Exemplo de Uso da API C#

```csharp
using MeshSectionLoops;

// 1. Cria malha triangular (Tetraedro)
Point3I[] vertices = [
    new(0, 0, 0), new(2, 0, 0), new(0, 2, 0), new(0, 0, 2)
];
TriangleIndices[] triangles = [
    new(0, 2, 1), new(0, 1, 3), new(0, 3, 2), new(1, 2, 3)
];
var mesh = new MeshSurface(vertices, triangles);

// 2. Define plano de corte: z = 1 (normal (0, 0, 1), offset 1)
var plane = new Plane3I(0, 0, 1, 1);

// 3. Executa o corte exato
var result = SectionEngine.SectionMesh(mesh, plane);

if (result.Status == SectionStatus.CompleteLoops)
{
    Console.WriteLine($"Contornos gerados: {result.Loops.Count}");
    var loop = result.Loops[0];
    Console.WriteLine($"Vértices no contorno: {loop.Points.Count}");
    Console.WriteLine($"Área projetada exata: {loop.ProjectedSignedArea}"); // 1/2
}
```

---

## Documentação Técnica e Formal

Para demonstrações detalhadas, equações e garantias topológicas:
- [Especificação de Contrato](docs/contract.md): Envelope de distâncias, invariantes de 2-variedades e álgebra racional.
- [Arquitetura e Design](docs/design.md): Mapas de incidência de meio-arestas, ciclos e simplificação colinear.

---

## Segurança

Para reportar vulnerabilidades, utilize o Private Vulnerability Reporting do GitHub ou consulte [SECURITY.md](SECURITY.md).

---

## Licença

Este projeto é distribuído sob a [Licença MIT](LICENSE).
Copyright © 2026 Lofrano Tecnologia.

# Cone Beyond the Doors

### Estrutura de Diretórios

> [!TIP]
> Essa estrutura não é concreta e pode ser modificada ao longo do desenvolvimento do projeto, use ela como um guia.
> Caso haja modificação, atualizar o modelo.

```
Assets/
├── Art/                  # Recursos visuais (sprites, fundos, arte de UI)
│   ├── Textures/         # Texturas para utilização, organizadas por categoria ou modelo
│   └── Models/           # Modelos 3D, organizados por categoria
├── Fonts/                # Arquivos de fontes e assets para textos na UI
├── Resources/            # Prefabs, ScriptableObjects e assets para carregamento em tempo de execução
│   ├── Prefabs/          # Prefabs reutilizáveis de objetos do jogo
│   └── ScriptableObjects/# Assets de ScriptableObject para configuração/dados
├── Scenes/               # Arquivos de cenas do Unity
├── Scripts/              # Todos os scripts C#, organizados por tipo e função
│   ├── Classes/          # Classes principais de gameplay/dados
│   ├── Controllers/      # Scripts para controlar objetos do jogo
│   ├── Enums/            # Definições de enums e interfaces
│   ├── Gameplay/         # Lógica de gameplay (efeitos, poderes, etc.)
│   ├── HUD/              # Scripts de UI (HUD, menus, etc.)
│   ├── Manager/          # Scripts de gerenciamento do jogo
│   └── Structures/       # Estruturas de dados e utilitários
├── Settings/             # Configurações do projeto, renderização e templates de cena
└── TextMesh Pro/         # Fontes e recursos do TextMesh Pro
```

## Descrição das Pastas

- **Art/**: Recursos visuais como modelos, fundos e elementos de interface.
- **Fonts/**: Arquivos de fontes e assets para renderização de texto.
- **Resources/**: Prefabs e ScriptableObjects para carregamento e configuração em tempo de execução.
- **Scenes/**: Arquivos de cenas do Unity para diferentes ambientes do jogo.
- **Scripts/**: Todos os scripts C#, organizados por gameplay, UI, gerenciamento e estruturas de dados.
- **Settings/**: Configurações do projeto, assets de renderização e templates.
- **TextMesh Pro/**: Assets e recursos para renderização avançada de texto.

### Coding Conventions

É recomendado que se utilize as normas de C# dadas pela Microsoft.
[Normas dadas pela Microsoft](https://learn.microsoft.com/pt-br/dotnet/csharp/fundamentals/coding-style/coding-conventions).

### Regras de Commit

Por favor aderir o máximo possivel.

> [!NOTE]
> Os commits devem seguir o padrão:
>
> ```
> tipo(especifico): descrição
> ```

Os tipos seguem a convenção dada em [ConventionalCommits](https://www.conventionalcommits.org/pt-br/v1.0.0/). Versão 1.0.0

### Regras de Branch

> [!NOTE]
> A Nomeclatura das Branches deve seguir o padrão:
>
> ```
> tipo/descrição
> ```

Os tipos seguem a convenção dada em [ConventionalBranches](https://conventional-branch.github.io/pt-br/). Versão 1.0.0
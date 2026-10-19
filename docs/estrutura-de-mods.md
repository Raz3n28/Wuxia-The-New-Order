# Como os mods de Order: The New Dawn funcionam

> Levantado em 19/10/2026 lendo os mods do Workshop instalados no PC (Harmony, UI Example oficial, Database Search, Walk & Diagonal Build, No Mad Shutdown, Dressing Table, Unlimited Stacking) e os mods de tradução do Paulo. A versão do jogo era a de 18/10/2026.

## Estrutura de pastas

```
<Mod>/
  Manifest/
    manifest.xml      obrigatório
    icon.png          obrigatório (sem ele a lista de mods pisca um bloco branco)
    preview.png       obrigatório
  v_common/           pasta da "versão"; o nome vem do <version> do manifest
    Assemblies/       DLLs do mod (a do <main>, mais as do <lib>)
    Resources/
      Def/def.xml     definições no formato do jogo (ModXmlDef)
      Textures/       imagens carregadas pelo código
      AssetBundle/    prefabs Unity (UI, por exemplo)
    config.json       configuração própria do mod (cada mod define a sua)
```

Mods locais ficam em `...\Outer Space\mods\<Pasta>`; os do Workshop em `...\steamapps\workshop\content\3080070\<id>`. A lista de mods ativos fica em `%USERPROFILE%\AppData\LocalLow\CatStudio\Outer Space\mod.dat`, e o log em `Player.log` na mesma pasta.

## manifest.xml

```xml
<ModXmlManifest>
  <packageId>autor.nome</packageId>
  <name>Nome</name>
  <author>Autor</author>
  <icon>icon.png</icon>
  <previewImage>preview.png</previewImage>
  <desc>Descrição</desc>
  <versions>
    <ModMetaVersion>
      <version>v_common</version>
      <dependencies>com.harmony</dependencies>  <!-- packageId de outro mod -->
      <lib>0Harmony.dll</lib>                   <!-- opcional -->
      <main>MeuMod.dll</main>
    </ModMetaVersion>
  </versions>
</ModXmlManifest>
```

- **Nunca** deixe um elemento vazio (`<lib />`): o jogo tenta abrir a pasta Assemblies como arquivo e quebra o carregamento de todos os mods.
- O `name` aparece igual em qualquer idioma, então vale escrever nos dois idiomas.

## Ponto de entrada (código)

O carregador procura uma classe com nome fixo, **no namespace global**:

```csharp
public class ModPluginLauncher : BaseModLauncher
{
    public override void launcher() { /* início do mod */ }
}
```

- `config` (do tipo `ModXmlManifest`) vem preenchido; tem `packageId` e `getRootPath(...)` (o jogo usa `config.getRootPath("Manifest", config.icon)`).
- Os mods costumam criar um `GameObject` com `DontDestroyOnLoad` e um `MonoBehaviour` para rodar a cada frame.
- Os mods oficiais usam Harmony (`[HarmonyPatch]` + `PatchAll`) e são compilados para **.NET Framework 4.7.1**. O Harmony do Workshop é a versão **2.4.2**.

## Definições do jogo (def.xml)

O jogo lê linhas de tabelas pelo nome. Exemplo oficial (texto multilíngue):

```xml
<ModXmlDef>
  <rows>
    <ModDefRow Name="LanguageDef">
      <Item Name="Key">minha_chave</Item>
      <Item Name="CN">中文</Item>
      <Item Name="EN">English</Item>
    </ModDefRow>
  </rows>
</ModXmlDef>
```

O jogo só tem as colunas CN e EN. O português do mod fica no nosso próprio sistema (`v_common/Languages`).

**Pendente:** descobrir os nomes das outras tabelas (itens, receitas, construções, personagens, inimigos). Os mods de conteúdo (Mech Expansion, Robot Forge, Crematory) devem mostrar isso.

## Classes do jogo já conhecidas (dos mods com código aberto)

| Classe / membro | Para quê |
|---|---|
| `MapManager.instance.heroManager.getAllRoles()` | Lista de personagens |
| `FWHeroBean.getMood()` / `addMood` | Humor do personagem |
| `R_BaseCharacter_Def.isAndroid()` | Robôs/androides |
| `allianceID == 1` | Personagem do jogador |
| `BaseAction`, `RolePlan.createPlan(id)` | Ações e planos da IA dos personagens |
| `AI_DailyHero` | IA do dia a dia |
| `ViewCover` | Tela inicial (o UI Example mexe nela) |
| `BaseMonoDialog`, `showModDialog` | Janelas de UI |
| `TextureManager.loadImage(...)` | Carregar imagens do mod |
| `MutiLanguage` | Sistema de idiomas do jogo |
| `ChannelSteam.Instance.ugc` | Publicar no Workshop |

## Compilar

Os outros autores usam `csc.exe` do Windows (só C# 5) ou o .NET SDK. Este repositório usa o .NET SDK (`dotnet build`), que aceita C# moderno e gera DLL para .NET 4.7.1.

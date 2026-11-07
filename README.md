# VOTAÇÃO – Sistema Eletrônico de Votação (.NET 8 & Microsoft Access MDB)

Solução corporativa desenvolvida em **C# / .NET 8**, interface **Windows Forms**, arquitetura desacoplada em **5 camadas**, injeção de dependência nativa e persistência em banco **Microsoft Access (.MDB)**.

## Estrutura da Solução
- **VOTACAO.Domain**: Entidades, Enums, DTOs, Exceções e Interfaces de repositório.
- **VOTACAO.Infrastructure**: Conexão MDB resiliente (ACE 12.0 / Jet 4.0), Logging diário estruturado e Configurações.
- **VOTACAO.Data**: Repositórios ADO.NET/OleDb e motor próprio de migrações e versionamento incremental do MDB.
- **VOTACAO.BO**: Camada de regras de negócio, validações de unicidade, transações ACID e apuração de votos.
- **VOTACAO.UI**: Windows Forms, formulário Principal com layout fixo, navegação desacoplada por UserControls.

## Como Abrir e Executar no Visual Studio 2022
1. Dê um duplo clique no arquivo de solução `VOTACAO.sln`.
2. Certifique-se de compilar na plataforma **x86** (configuração padrão para compatibilidade máxima com drivers OLE DB do Access/MDB).
3. Pressione **F5** ou clique em **Iniciar**.
4. O sistema criará automaticamente a pasta `Database/` e o arquivo físico `Votacao.mdb` com todas as tabelas, índices e dados demonstrativos iniciais.

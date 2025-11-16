using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace rosalia.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ILogger<ClientesController> _logger;

        private const string StrConex = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=SistemaClientes;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

        public ClientesController(ILogger<ClientesController> logger)
        {
            _logger = logger;
        }

        [HttpGet("buscar")]
        public IActionResult BuscarClientes(string filtro = "TODOS", string texto = "")
        {
            using (SqlConnection conection = new SqlConnection(StrConex))
            {
                conection.Open();
                try
                {
                    texto = texto?.Trim() ?? "";

                    string sql = @"
            SELECT 
                C.Id_Cliente,
                C.Tipo_Cliente,
                C.Nome_RazaoSocial,
                C.Email,
                C.Telefone,
                C.Endereco,
                D.CPF,
                D.RG,
                D.Data_Nascimento,
                D.CNPJ,
                D.Inscricao_Estadual,
                D.Nome_Responsavel,
                D.Data_Abertura
            FROM CLIENTE C
            LEFT JOIN DETALHES_PF_PJ D ON C.Id_Cliente = D.Id_Cliente
            WHERE 
                (@Filtro = 'TODOS' OR C.Tipo_Cliente = @Filtro)
                AND (
                    C.Nome_RazaoSocial LIKE '%' + @Texto + '%'
                    OR D.CPF LIKE '%' + @Texto + '%'
                    OR D.CNPJ LIKE '%' + @Texto + '%'
                    OR C.Email LIKE '%' + @Texto + '%'
                )";

                    var cmd = new SqlCommand(sql, conection);

                    cmd.Parameters.AddWithValue("@Filtro", filtro);
                    cmd.Parameters.AddWithValue("@Texto", texto);

                    var lista = new List<Dictionary<string, object>>();

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var item = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                            item.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));

                        lista.Add(item);
                    }

                    return Ok(lista);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { erro = ex.Message });
                }
            }
        }

        [HttpPost]
        public IActionResult Cadastrar([FromBody] Clientes cli)
        {
            try
            {
                using var conection = new SqlConnection(StrConex);
                conection.Open();

                // Passo 1 - Inserir Cliente base
                var cmdCliente = new SqlCommand(@"
            INSERT INTO CLIENTE 
            (Tipo_Cliente, Nome_RazaoSocial, Email, Telefone, Endereco)
            VALUES (@Tipo, @Nome, @Email, @Telefone, @Endereco);
            SELECT SCOPE_IDENTITY();", conection);

                cmdCliente.Parameters.AddWithValue("@Tipo", cli.Tipo);
                cmdCliente.Parameters.AddWithValue("@Nome", cli.Nome);
                cmdCliente.Parameters.AddWithValue("@Email", cli.Email);
                cmdCliente.Parameters.AddWithValue("@Telefone", cli.Telefone);
                cmdCliente.Parameters.AddWithValue("@Endereco", (object?)cli.Endereco ?? DBNull.Value);

                int idCliente = Convert.ToInt32(cmdCliente.ExecuteScalar());

                // Passo 2 - Inserir tabela DETALHES_PF_PJ
                var cmdDetalhes = new SqlCommand(@"
            INSERT INTO DETALHES_PF_PJ
            (Id_Cliente, CPF, RG, Data_Nascimento, CNPJ, Inscricao_Estadual, Nome_Responsavel, Data_Abertura)
            VALUES
            (@IdCliente, @CPF, @RG, @DataNascimento, @CNPJ, @InscricaoEstadual, @NomeResponsavel, @DataAbertura)", conection);

                cmdDetalhes.Parameters.AddWithValue("@IdCliente", idCliente);
                cmdDetalhes.Parameters.AddWithValue("@CPF", (object?)cli.CPF ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@RG", (object?)cli.RG ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@DataNascimento", (object?)cli.DataNascimento ?? DBNull.Value);

                cmdDetalhes.Parameters.AddWithValue("@CNPJ", (object?)cli.CNPJ ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@InscricaoEstadual", (object?)cli.InscricaoEstadual ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@NomeResponsavel", (object?)cli.NomeResponsavel ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@DataAbertura", (object?)cli.DataAbertura ?? DBNull.Value);

                cmdDetalhes.ExecuteNonQuery();

                return Ok(new { mensagem = "Cliente cadastrado com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult Atualizar(int id, [FromBody] Clientes cli)
        {
            try
            {
                using var conection = new SqlConnection(StrConex);
                conection.Open();

                // Passo 1 - Atualizar Cliente base
                var cmdCliente = new SqlCommand(@"
                    UPDATE CLIENTE 
                    SET Tipo_Cliente = @Tipo,
                        Nome_RazaoSocial = @Nome,
                        Email = @Email,
                        Telefone = @Telefone,
                        Endereco = @Endereco
                    WHERE Id_Cliente = @Id", conection);

                cmdCliente.Parameters.AddWithValue("@Id", id);
                cmdCliente.Parameters.AddWithValue("@Tipo", cli.Tipo);
                cmdCliente.Parameters.AddWithValue("@Nome", cli.Nome);
                cmdCliente.Parameters.AddWithValue("@Email", cli.Email);
                cmdCliente.Parameters.AddWithValue("@Telefone", cli.Telefone);
                cmdCliente.Parameters.AddWithValue("@Endereco", (object?)cli.Endereco ?? DBNull.Value);

                int linhasAfetadas = cmdCliente.ExecuteNonQuery();

                if (linhasAfetadas == 0)
                {
                    return NotFound(new { erro = "Cliente nao encontrado" });
                }

                // Passo 2 - Atualizar DETALHES_PF_PJ
                var cmdDetalhes = new SqlCommand(@"
                    UPDATE DETALHES_PF_PJ
                    SET CPF = @CPF,
                        RG = @RG,
                        Data_Nascimento = @DataNascimento,
                        CNPJ = @CNPJ,
                        Inscricao_Estadual = @InscricaoEstadual,
                        Nome_Responsavel = @NomeResponsavel,
                        Data_Abertura = @DataAbertura
                    WHERE Id_Cliente = @IdCliente", conection);

                cmdDetalhes.Parameters.AddWithValue("@IdCliente", id);
                cmdDetalhes.Parameters.AddWithValue("@CPF", (object?)cli.CPF ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@RG", (object?)cli.RG ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@DataNascimento", (object?)cli.DataNascimento ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@CNPJ", (object?)cli.CNPJ ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@InscricaoEstadual", (object?)cli.InscricaoEstadual ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@NomeResponsavel", (object?)cli.NomeResponsavel ?? DBNull.Value);
                cmdDetalhes.Parameters.AddWithValue("@DataAbertura", (object?)cli.DataAbertura ?? DBNull.Value);

                cmdDetalhes.ExecuteNonQuery();

                return Ok(new { mensagem = "Cliente atualizado com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Excluir(int id)
        {
            try
            {
                using var conection = new SqlConnection(StrConex);
                conection.Open();

                // Passo 1 - Excluir DETALHES primeiro (por causa da FK)
                var cmdDetalhes = new SqlCommand(@"
                    DELETE FROM DETALHES_PF_PJ 
                    WHERE Id_Cliente = @Id", conection);
                cmdDetalhes.Parameters.AddWithValue("@Id", id);
                cmdDetalhes.ExecuteNonQuery();

                // Passo 2 - Excluir CLIENTE
                var cmdCliente = new SqlCommand(@"
                    DELETE FROM CLIENTE 
                    WHERE Id_Cliente = @Id", conection);
                cmdCliente.Parameters.AddWithValue("@Id", id);

                int linhasAfetadas = cmdCliente.ExecuteNonQuery();

                if (linhasAfetadas == 0)
                {
                    return NotFound(new { erro = "Cliente nao encontrado" });
                }

                return Ok(new { mensagem = "Cliente excluido com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

    }
}
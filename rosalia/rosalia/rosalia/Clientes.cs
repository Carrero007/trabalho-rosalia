namespace rosalia
{
    public class Clientes
    {
        public string Tipo { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string Endereco { get; set; }

        // PF
        public string CPF { get; set; }
        public string RG { get; set; }
        public DateTime? DataNascimento { get; set; }

        // PJ
        public string CNPJ { get; set; }
        public string InscricaoEstadual { get; set; }
        public string NomeResponsavel { get; set; }
        public DateTime? DataAbertura { get; set; }
    }
}

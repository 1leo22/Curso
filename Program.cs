using System;
using System.Diagnostics;
using System.Linq;
using Curso.Data;
using Curso.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DominandoEFCore
{
    class Program
    {
        static void Main(string[] args)
        {
			//EnsureCreatedAndDeleted();
			
			//GapEnsureCreated();
			
			//HealthCheckModoAntigo();
			
			//HealthCheckNovoModo();
			
			//GerenciarEstadoDaConexao();

			//SqlInjection();
		}
		static void SqlInjection()
		{
			using var db = new ApplicationContext();
			db.Database.EnsureDeleted();
			db.Database.EnsureCreated();

			db.Departamentos.AddRange(
				new Departamento()
				{
					Descricao = "Departamento 01"
				},
				new Departamento()
				{
					Descricao = "Departamento 02"
				}
			);

			db.SaveChanges();

			//Forma correta de realizar query. Utilizando de queryparameters
			var descricao = "Departamento 01";
			db.Database.ExecuteSqlRaw("update Departamentos set descricao='DepartamentoAlterado' where descricao={0}", descricao);
			
			foreach(var itm in db.Departamentos.AsNoTracking())
			{
				Console.WriteLine($"Id: {itm.Id} - Descrição: {itm.Descricao} - Forma correta de query");
			}

			//SqlInjection
			var descricaoInjection = "Teste ' or 1='1";

			db.Database.ExecuteSqlRaw($"update Departamentos set descricao='SqlInjection' where descricao='{descricaoInjection}'");
			foreach(var itm in db.Departamentos.AsNoTracking())
			{
				Console.WriteLine($"Id: {itm.Id} - Descrição: {itm.Descricao} - Gerou sql injection");
			}
		}

		static void GerenciarEstadoDaConexao()
		{
			//warmup
			new ApplicationContext().Departamentos.AsNoTracking().Any();
			_count = 0;
			GerenciarEstadoDaConexao(false);
			_count = 0;
			GerenciarEstadoDaConexao(true);
		}

		static int _count;
		static void GerenciarEstadoDaConexao(bool gerenciarEstadoDaConexao)
		{
			using var db = new ApplicationContext();
			var time = Stopwatch.StartNew();

			var conexao = db.Database.GetDbConnection();
			conexao.StateChange += (_, _) => ++_count;

			if(gerenciarEstadoDaConexao)
			{
				conexao.Open();
			}

			for(int i = 0; i < 2000; i++)
			{
				db.Departamentos.AsNoTracking().Any();
			}

			time.Stop();
			var mensagem = $"Tempo: {time.Elapsed}, {gerenciarEstadoDaConexao}, Contador: {_count}";

			Console.WriteLine(mensagem);
		}

		static void HealthCheckNovoModo()
		{
			using var db = new ApplicationContext();

			var canConnect = db.Database.CanConnect();

			if(canConnect)
			{
				Console.WriteLine("Conectado...");
			}
			else
			{
				Console.WriteLine($"Erro ao conectar na base de dados.");
			}
			//Essa validação não me permite ver qual o erro ao se conectar a base
		}

		static void HealthCheckModoAntigo()
		{
			using var db = new ApplicationContext();
			try
			{
				
				var connection = db.Database.GetDbConnection();
				connection.Open();

				Console.WriteLine("Conectado...");

			}catch(Exception e)
			{
				Console.WriteLine($"Erro ao conectar na base de dados. Error: {e.Message}");
			}
		}

		static void EnsureCreatedAndDeleted()
		{
			using var db = new ApplicationContext();
			db.Database.EnsureDeleted();//garante que a base esteja deletada ao rodar a aplicação
			db.Database.EnsureCreated();//garante que a base seja criada ao rodar a aplicação
		}

		static void GapEnsureCreated()
		{
			using var db1 = new ApplicationContext();
			using var db2 = new ApplicationContextCidade();
			
			db1.Database.EnsureCreated();
			db2.Database.EnsureCreated();//Não cria a tabela cidade, pois a ação valida se a base ja existe
										 //se existir, não executa.
			
			//para forçar a execução das ações faltantes é preciso executar o código abaixo
			var databaseCreator = db2.GetService<IRelationalDatabaseCreator>();
			databaseCreator.CreateTables();
		}
    }
}

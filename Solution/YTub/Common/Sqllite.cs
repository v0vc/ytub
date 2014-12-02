using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace YTub.Common
{
    public class Sqllite
    {
        private const string TableVideos = "tblVideos";

        private const string TableSettings = "tblSettings";

        public static string AppDir;

        public static void CreateOrConnectDb(string dbfile, string autor, out int totalrow)
        {
            totalrow = 0;
            var fn = new FileInfo(dbfile);

            if (fn.Exists)
            {
                if (fn.Length == 0)
                {
                    fn.Delete();
                    CreateDb(fn.FullName);
                }
                var zap = string.Format("SELECT * FROM {0} WHERE chanelowner='{1}'", TableVideos, autor);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", fn.FullName)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        totalrow += sdr.Cast<DbDataRecord>().Count();
                    }
                    sqlcon.Close();
                }
            }
            else
            {
                CreateDb(fn.FullName);
            }
        }

        public static void InsertRecord(string dbfile, string id, string chanelowner, string chanelname, string servername, int ordernum, int isfavorite,
            string url, string title, int viewcount, int previewcount, int duration, DateTime published,
            string description)
        {
            Task.Run(() =>
            {
                title = title.Replace("'", "''");
                chanelowner = chanelowner.Replace("'", "''");
                var zap =
                    string.Format(
                        @"INSERT INTO '{0}' ('v_id', 'chanelowner', 'chanelname', 'servername', 'ordernum', 'isfavorite', 'url', 'title', 'viewcount', 'previewcount', 'duration', 'published', 'description', 'cleartitle') 
                                VALUES (@v_id, @chanelowner, @chanelname, @servername, @ordernum, @isfavorite, @url, @title, @viewcount, @previewcount, @duration, @published, @description, @cleartitle)",
                        TableVideos);
                using (
                    var sqlcon =
                        new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile))
                    )
                using (var sqlcommand = new SQLiteCommand(sqlcon))
                {
                    sqlcommand.CommandText = zap;
                    sqlcommand.Parameters.AddWithValue("@v_id", id);
                    sqlcommand.Parameters.AddWithValue("@chanelowner", chanelowner);
                    sqlcommand.Parameters.AddWithValue("@chanelname", chanelname);
                    sqlcommand.Parameters.AddWithValue("@servername", servername);
                    sqlcommand.Parameters.AddWithValue("@ordernum", ordernum);
                    sqlcommand.Parameters.AddWithValue("@isfavorite", isfavorite);
                    sqlcommand.Parameters.AddWithValue("@url", url);
                    sqlcommand.Parameters.AddWithValue("@title", title);
                    sqlcommand.Parameters.AddWithValue("@viewcount", viewcount);
                    sqlcommand.Parameters.AddWithValue("@previewcount", previewcount);
                    sqlcommand.Parameters.AddWithValue("@duration", duration);
                    sqlcommand.Parameters.AddWithValue("@published", published);
                    sqlcommand.Parameters.AddWithValue("@description", description);
                    sqlcommand.Parameters.AddWithValue("@cleartitle", VideoItem.MakeValidFileName(title));
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
        }

        public static bool IsTableHasRecord(string dbfile, string id)
        {
            var res = false;
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0} WHERE v_id='{1}'", TableVideos, id);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        res = sdr.HasRows;
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static Dictionary<string, string> GetDistinctValues(string dbfile, string chanelowner, string chanelname)
        {
            var res = new Dictionary<string, string>();
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT DISTINCT {0}, {1}, servername, ordernum FROM {2} ORDER BY ordernum ASC", chanelowner, chanelname, TableVideos);
                using (
                    var sqlcon =
                        new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        foreach (DbDataRecord record in sdr)
                        {
                            res.Add(record[chanelowner].ToString(), record[chanelname] + ":" + record["servername"] + ":" + record["ordernum"]);
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static List<DbDataRecord> GetChanelVideos(string dbfile, string chanelowner)
        {
            var res = new List<DbDataRecord>();
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0} WHERE chanelowner='{1}'", TableVideos, chanelowner);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        res.AddRange(sdr.Cast<DbDataRecord>());
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static string GetSettingsValue(string dbfile, string settingname)
        {
            var res = string.Empty;
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0}", TableSettings);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        if (sdr.HasRows)
                        {
                            while (sdr.Read())
                            {
                                try
                                {
                                    res = sdr[settingname].ToString();
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(string.Format("Check db: {0}{1} {2}", settingname,
                                        Environment.NewLine,
                                        ex.Message));
                                }

                                break;
                            }
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static int GetSettingsIntValue(string dbfile, string settingname)
        {
            var res = 0;
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0}", TableSettings);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        if (sdr.HasRows)
                        {
                            while (sdr.Read())
                            {
                                int resu;
                                if (int.TryParse(sdr[settingname].ToString(), out resu))
                                {
                                    res = resu;
                                }
                                break;
                            }
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static int GetVideoIntValue(string dbfile, string settingname, string keyfield, string key)
        {
            var res = 0;
            Task t = Task.Run(() =>
            {
                var zap = string.Format("SELECT * FROM {0} WHERE {1}='{2}'", TableVideos, keyfield, key);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    using (var sdr = sqlcommand.ExecuteReader())
                    {
                        if (sdr.HasRows)
                        {
                            while (sdr.Read())
                            {
                                int resu;
                                if (int.TryParse(sdr[settingname].ToString(), out resu))
                                {
                                    res = resu;
                                }
                                break;
                            }
                        }
                    }
                    sqlcon.Close();
                }
            });
            t.Wait();
            return res;
        }

        public static void UpdateSetting(string dbfile, string settingname, object settingvalue)
        {
            Task.Run(() =>
            {
                var zap = string.Format("UPDATE {0} SET {1}='{2}'", TableSettings, settingname, settingvalue);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
        }

        public static void UpdateChanelOrder(string dbfile, string chanelowner, int neworder)
        {
            Task.Run(() =>
            {
                var zap = string.Format("UPDATE {0} SET ordernum='{1}' WHERE chanelowner='{2}'", TableVideos, neworder,
                    chanelowner);
                using (
                    var sqlcon =
                        new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
        }

        public static void CreateSettings(string dbfile, string tablename, Dictionary<string, string> columns)
        {
            Task t = Task.Run(() =>
            {
                var sb = new StringBuilder(string.Format("INSERT INTO '{0}' (", tablename));
                foreach (KeyValuePair<string, string> column in columns)
                {
                    sb.AppendFormat("'{0}',", column.Key);
                }
                var tmp = sb.ToString().TrimEnd(',') + ") VALUES (";
                sb = new StringBuilder(tmp);
                foreach (KeyValuePair<string, string> column in columns)
                {
                    sb.AppendFormat("'{0}',", column.Value);
                }

                var zap = sb.ToString().TrimEnd(',') + ")";
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void UpdateValue(string dbfile, string valuename, string keyfield, string key, object value)
        {
            Task.Run(() =>
            {
                var zap = string.Format("UPDATE {0} SET {1}='{2}' WHERE {3}='{4}'", TableVideos, valuename, value,
                    keyfield, key);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
        }

        public static void RemoveChanelFromDb(string dbfile, string chanelowner)
        {
            Task.Run(() =>
            {
                var zap = string.Format("DELETE FROM {0} WHERE chanelowner='{1}'", TableVideos, chanelowner);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
        }

        public static void UpdateChanelName(string dbfile, string newname, string chanelowner)
        {
            Task.Run(() =>
            {
                var zap = string.Format("UPDATE {0} SET chanelname='{1}' WHERE chanelowner='{2}'", TableVideos, newname, chanelowner);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
        }

        public static void DropTable(string dbfile, string tablename)
        {
            Task t = Task.Run(() =>
            {
                var zap = string.Format("DROP TABLE {0}", tablename);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void CreateTable(string dbfile, string tablename, Dictionary<string, string> columns)
        {
            Task t =Task.Run(() =>
            {
                var sb = new StringBuilder(string.Format("CREATE TABLE {0} (", tablename));
                foreach (KeyValuePair<string, string> column in columns)
                {
                    sb.AppendFormat("{0} {1},", column.Key, column.Value);
                }
                var zap = sb.ToString().TrimEnd(',') + ")";
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                using (var sqlcommand = new SQLiteCommand(zap, sqlcon))
                {
                    sqlcon.Open();
                    sqlcommand.ExecuteNonQuery();
                    sqlcon.Close();
                }
            });
            t.Wait();
        }

        public static void CreateDb(string dbfile)
        {
            Task.Run(() =>
            {
                var fnyoudl = new FileInfo(Path.Combine(AppDir, "youtube-dl", "youtube-dl.exe"));
                var fnffmpeg = new FileInfo(Path.Combine(AppDir, "ffmpeg", "ffmpeg.exe"));
                SQLiteConnection.CreateFile(dbfile);
                var lstcom = new List<string>();
                var zap = string.Format(@"CREATE TABLE {0} (v_id TEXT PRIMARY KEY,
                                                        chanelowner TEXT,
                                                        chanelname TEXT,
                                                        servername TEXT,
                                                        ordernum INT,
                                                        isfavorite INT,
                                                        url TEXT,
                                                        title TEXT,
                                                        viewcount INT,
                                                        previewcount INT,
                                                        duration INT,
                                                        published DATETIME,
                                                        description TEXT,
                                                        cleartitle TEXT)", TableVideos);
                lstcom.Add(zap);
                var zapdir = string.Format(@"CREATE TABLE {0} (savepath TEXT, 
                                                            pathtompc TEXT, 
                                                            synconstart INT, 
                                                            pathtoyoudl TEXT, 
                                                            pathtoffmpeg TEXT, 
                                                            isonlyfavor INT, 
                                                            ispopular INT,
                                                            asyncdl INT,
                                                            culture TEXT,
                                                            rtlogin TEXT,
                                                            rtpassword TEXT,
                                                            taplogin TEXT,
                                                            tappassword TEXT)",
                        TableSettings);
                lstcom.Add(zapdir);
                string insdir;
                if (fnyoudl.Exists & fnffmpeg.Exists)
                {
                    insdir = string.Format(@"INSERT INTO '{0}' ('savepath', 'synconstart', 'isonlyfavor', 'ispopular', 'asyncdl', 'pathtoyoudl', 'pathtoffmpeg', 'culture') 
                                                VALUES ('{1}', '0', '0', '0', '1', '{2}', '{3}', 'RU')", 
                                                TableSettings, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fnyoudl.FullName, fnffmpeg.FullName);
                }
                else
                {
                    insdir = string.Format(@"INSERT INTO '{0}' ('savepath', 'synconstart', 'isonlyfavor', 'ispopular', 'asyncdl', 'culture') 
                                                VALUES ('{1}', '0', '0', '0', '1', 'RU')",
                                                TableSettings, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));    
                }
                lstcom.Add(insdir);
                using (var sqlcon = new SQLiteConnection(string.Format("Data Source={0};Version=3;FailIfMissing=True", dbfile)))
                foreach (string com in lstcom)
                {
                    using (var sqlcommand = new SQLiteCommand(com, sqlcon))
                    {
                        sqlcon.Open();
                        sqlcommand.ExecuteNonQuery();
                        sqlcon.Close();
                    }
                }
            });
        }
    }
}

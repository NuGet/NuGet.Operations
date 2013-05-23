using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet;

namespace RenderBlobs
{
    static class SqlGallery
    {
        public static Gallery Load(string connectionString)
        {
            Gallery gallery = new Gallery();

            LoadPackageRegistrations(connectionString, gallery);

            Console.WriteLine("LoadPackageRegistrations complete");

            LoadOwners(connectionString, gallery);

            Console.WriteLine("LoadOwners complete");
            
            LoadPackages(connectionString, gallery);

            Console.WriteLine("LoadPackages complete");

            LoadPackageDependencies(connectionString, gallery);

            Console.WriteLine("LoadPackageDependencies complete");

            LoadPackageAuthors(connectionString, gallery);

            Console.WriteLine("LoadPackageAuthors complete");

            return gallery;
        }

        private static void LoadPackageRegistrations(string connectionString, Gallery gallery)
        {
            string sql = @"
                SELECT DISTINCT
                    PackageRegistrations.[Id] 'Id',
                    PackageRegistrations.[DownloadCount] 'DownloadCount'
                FROM PackageRegistrations
                INNER JOIN Packages ON Packages.[PackageRegistrationKey] = PackageRegistrations.[Key]
            ";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sql, connection);

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string id = (string)reader.GetValue(0);
                    int downloadCount = (int)reader.GetValue(1);

                    Gallery.PackageRegistration packageRegistration = new Gallery.PackageRegistration();

                    packageRegistration.Id = id;
                    packageRegistration.DownloadCount = downloadCount;

                    gallery.PackageRegistrations.Add(id, packageRegistration);
               }
            }
        }
        
        private static void LoadOwners(string connectionString, Gallery gallery)
        {
            string sql = @"
                SELECT DISTINCT Users.[Username], PackageRegistrations.[Id]
                FROM Users
                INNER JOIN PackageRegistrationOwners ON PackageRegistrationOwners.[UserKey] = [Users].[Key]
                INNER JOIN PackageRegistrations ON PackageRegistrationOwners.[PackageRegistrationKey] = PackageRegistrations.[Key]
                INNER JOIN Packages ON Packages.[PackageRegistrationKey] = PackageRegistrations.[Key]
            ";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sql, connection);

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string userName = (string)reader.GetValue(0);
                    string packageRegistrationId = (string)reader.GetValue(1);

                    Gallery.Owner owner;
                    if (!gallery.Owners.TryGetValue(userName, out owner))
                    {
                        owner = new Gallery.Owner();

                        owner.UserName = userName;

                        gallery.Owners.Add(userName, owner);
                    }

                    Gallery.PackageRegistration packageRegistration = gallery.PackageRegistrations[packageRegistrationId];

                    owner.PackageRegistrations.Add(packageRegistrationId, packageRegistration);

                    packageRegistration.Owners.Add(owner.UserName, owner);
                }
            }
        }

        private static void LoadPackages(string connectionString, Gallery gallery)
        {
            string sql = @"
                SELECT
                PackageRegistrations.[Id],
                Copyright, 
                Created,
                Description,  
                Packages.DownloadCount, 
                ExternalPackageUrl, 
                HashAlgorithm,  
                Hash, 
                IconUrl, 
                IsLatest, 
                LastUpdated, 
                LicenseUrl, 
                Published, 
                PackageFileSize, 
                ProjectUrl, 
                RequiresLicenseAcceptance,  
                Summary, 
                Tags,  
                Title,  
                Version, 
                FlattenedAuthors,  
                FlattenedDependencies,  
                IsLatestStable,  
                Listed,  
                IsPrerelease,  
                ReleaseNotes,  
                Language 
                FROM Packages
                INNER JOIN PackageRegistrations ON PackageRegistrations.[Key] = Packages.[PackageRegistrationKey]
            ";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sql, connection);
                command.CommandType = CommandType.Text;

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string packageId = null;

                    try
                    {
                        string packageRegistrationId = (string)reader.GetValue(0);
                        string version = reader.IsDBNull(19) ? null : reader.GetValue(19).ToString();

                        packageId = string.Format("{0}/{1}", packageRegistrationId, version);

                        Gallery.Package package = new Gallery.Package();

                        package.Copyright = reader.IsDBNull(1) ? null : reader.GetValue(1).ToString();
                        package.Created = reader.GetDateTime(2);
                        package.Description = reader.IsDBNull(3) ? null : reader.GetValue(3).ToString();
                        package.DownloadCount = (int)reader.GetValue(4);
                        package.ExternalPackageUrl = reader.IsDBNull(5) ? null : SafeCreateUri(reader.GetValue(5).ToString());
                        package.HashAlgorithm = reader.IsDBNull(6) ? null : reader.GetValue(6).ToString();
                        package.Hash = reader.GetValue(7).ToString();
                        package.IconUrl = reader.IsDBNull(8) ? null : SafeCreateUri(reader.GetValue(8).ToString());
                        package.IsLatest = reader.GetBoolean(9);
                        package.LastUpdated = reader.GetDateTime(10);
                        package.LicenseUrl = reader.IsDBNull(11) ? null : SafeCreateUri(reader.GetValue(11).ToString());
                        package.Published = reader.GetDateTime(12);
                        package.PackageFileSize = (long)reader.GetValue(13);
                        package.ProjectUrl = reader.IsDBNull(14) ? null : SafeCreateUri(reader.GetValue(14).ToString());
                        package.RequiresLicenseAcceptance = reader.GetBoolean(15);
                        package.Summary = reader.IsDBNull(16) ? null : reader.GetValue(16).ToString();
                        package.Tags = reader.IsDBNull(17) ? null : reader.GetValue(17).ToString();
                        package.Title = reader.IsDBNull(18) ? null : reader.GetValue(18).ToString();
                        package.Version = reader.IsDBNull(19) ? null : reader.GetValue(19).ToString();
                        package.FlattenedAuthors = reader.IsDBNull(20) ? null : reader.GetValue(20).ToString();
                        package.FlattenedDependencies = reader.IsDBNull(21) ? null : reader.GetValue(21).ToString();
                        package.IsLatestStable = reader.GetBoolean(22);
                        package.Listed = reader.GetBoolean(23);
                        package.IsPrerelease = reader.GetBoolean(24);
                        package.ReleaseNotes = reader.IsDBNull(25) ? null : reader.GetValue(25).ToString();
                        package.Language = reader.IsDBNull(26) ? null : reader.GetValue(26).ToString();

                        Gallery.PackageRegistration packageRegistration = gallery.PackageRegistrations[packageRegistrationId];

                        package.PackageRegistration = packageRegistration;

                        packageRegistration.Packages.Add(package.Version, package);

                        gallery.Packages.Add(packageId, package);
                    }
                    catch (Exception e)
                    {
                        if (packageId != null)
                        {
                            throw new Exception(packageId, e);
                        }
                        throw;
                    }
                }
            }
        }

        private static void LoadPackageDependencies(string connectionString, Gallery gallery)
        {
            string sql = @"
                SELECT
                PackageRegistrations.[Id], 
                Packages.[Version], 
                PackageDependencies.[Id] 'Dependency', 
                PackageDependencies.[VersionSpec], 
                PackageDependencies.[TargetFramework] 
                FROM PackageDependencies
                INNER JOIN Packages ON PackageDependencies.[PackageKey] = Packages.[Key]
                INNER JOIN PackageRegistrations ON Packages.[PackageRegistrationKey] = PackageRegistrations.[Key]
            ";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sql, connection);
                command.CommandType = CommandType.Text;

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Gallery.PackageDependency packageDependency = new Gallery.PackageDependency();

                    string id = reader.GetValue(0).ToString();
                    string version = reader.GetValue(1).ToString();

                    string packageId = string.Format("{0}/{1}", id, version);

                    string dependency = reader.IsDBNull(2) ? null : reader.GetValue(2).ToString();

                    packageDependency.VersionSpec = reader.IsDBNull(3) ? null : reader.GetValue(3).ToString();
                    packageDependency.TargetFramework = reader.IsDBNull(4) ? null : reader.GetValue(4).ToString();

                    Gallery.PackageRegistration packageRegistration;
                    if (dependency != null && gallery.PackageRegistrations.TryGetValue(dependency, out packageRegistration))
                    {
                        packageDependency.Dependency = packageRegistration;

                        Gallery.Package package = gallery.Packages[packageId];

                        package.DependsOn.Add(packageDependency);
                    }
                }
            }
        }

        private static void LoadPackageAuthors(string connectionString, Gallery gallery)
        {
            string sql = @"
                SELECT PackageRegistrations.[Id], Packages.[Version], PackageAuthors.[Name]
                FROM PackageAuthors
                INNER JOIN Packages ON PackageAuthors.[PackageKey] = Packages.[Key]
                INNER JOIN PackageRegistrations ON Packages.[PackageRegistrationKey] = PackageRegistrations.[Key]
            ";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(sql, connection);
                command.CommandType = CommandType.Text;

                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Gallery.PackageDependency packageDependency = new Gallery.PackageDependency();

                    string id = reader.GetValue(0).ToString();
                    string version = reader.GetValue(1).ToString();
                    string author = reader.GetValue(2).ToString();

                    string packageId = string.Format("{0}/{1}", id, version);

                    gallery.Packages[packageId].Authors.Add(author);
                }
            }
        }

        private static Uri SafeCreateUri(string uri)
        {
            try
            {
                return new Uri(uri);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

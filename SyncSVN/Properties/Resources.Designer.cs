﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SyncSVN.Properties {
    using System;
    
    
    /// <summary>
    ///   Класс ресурса со строгой типизацией для поиска локализованных строк и т.д.
    /// </summary>
    // Этот класс создан автоматически классом StronglyTypedResourceBuilder
    // с помощью такого средства, как ResGen или Visual Studio.
    // Чтобы добавить или удалить член, измените файл .ResX и снова запустите ResGen
    // с параметром /str или перестройте свой проект VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Возвращает кэшированный экземпляр ResourceManager, использованный этим классом.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SyncSVN.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Перезаписывает свойство CurrentUICulture текущего потока для всех
        ///   обращений к ресурсу с помощью этого класса ресурса со строгой типизацией.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unable to read config file.
        /// </summary>
        public static string CONFIG_ERROR_READ {
            get {
                return ResourceManager.GetString("CONFIG_ERROR_READ", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Config file doens&apos;t contain requested fileds.
        /// </summary>
        public static string CONFIG_FIELDS_NOT_FOUND {
            get {
                return ResourceManager.GetString("CONFIG_FIELDS_NOT_FOUND", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Application settings file doesn&apos;t exists.
        /// </summary>
        public static string CONFIG_NOT_FOUND {
            get {
                return ResourceManager.GetString("CONFIG_NOT_FOUND", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unable to make checkout from repository.
        /// </summary>
        public static string UNABLE_CHECKOUT {
            get {
                return ResourceManager.GetString("UNABLE_CHECKOUT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unable to connect to svn server.
        /// </summary>
        public static string UNABLE_CONNECT {
            get {
                return ResourceManager.GetString("UNABLE_CONNECT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unable to download file.
        /// </summary>
        public static string UNABLE_DOWNLOAD {
            get {
                return ResourceManager.GetString("UNABLE_DOWNLOAD", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unable to pull content from repository.
        /// </summary>
        public static string UNABLE_PULL {
            get {
                return ResourceManager.GetString("UNABLE_PULL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unable push content to repository.
        /// </summary>
        public static string UNABLE_PUSH {
            get {
                return ResourceManager.GetString("UNABLE_PUSH", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unable to upload file.
        /// </summary>
        public static string UNABLE_UPLOAD {
            get {
                return ResourceManager.GetString("UNABLE_UPLOAD", resourceCulture);
            }
        }
    }
}

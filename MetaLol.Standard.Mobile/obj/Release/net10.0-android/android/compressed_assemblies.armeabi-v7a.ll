; ModuleID = 'compressed_assemblies.armeabi-v7a.ll'
source_filename = "compressed_assemblies.armeabi-v7a.ll"
target datalayout = "e-m:e-p:32:32-Fi8-i64:64-v128:64:128-a:0:32-n32-S64"
target triple = "armv7-unknown-linux-android21"

%struct.CompressedAssemblyDescriptor = type {
	i32, ; uint32_t uncompressed_file_size
	i1, ; bool loaded
	i32 ; uint32_t buffer_offset
}

@compressed_assembly_count = dso_local local_unnamed_addr constant i32 123, align 4

@compressed_assembly_descriptors = dso_local local_unnamed_addr global [123 x %struct.CompressedAssemblyDescriptor] [
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 0; uint32_t buffer_offset
	}, ; 0: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15144; uint32_t buffer_offset
	}, ; 1: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 30288; uint32_t buffer_offset
	}, ; 2: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 45432; uint32_t buffer_offset
	}, ; 3: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 60576; uint32_t buffer_offset
	}, ; 4: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 75720; uint32_t buffer_offset
	}, ; 5: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 90864; uint32_t buffer_offset
	}, ; 6: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 106008; uint32_t buffer_offset
	}, ; 7: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 121152; uint32_t buffer_offset
	}, ; 8: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 136296; uint32_t buffer_offset
	}, ; 9: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 151440; uint32_t buffer_offset
	}, ; 10: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 166584; uint32_t buffer_offset
	}, ; 11: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 181728; uint32_t buffer_offset
	}, ; 12: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 196872; uint32_t buffer_offset
	}, ; 13: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 212056; uint32_t buffer_offset
	}, ; 14: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 227200; uint32_t buffer_offset
	}, ; 15: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 242344; uint32_t buffer_offset
	}, ; 16: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 257488; uint32_t buffer_offset
	}, ; 17: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 272632; uint32_t buffer_offset
	}, ; 18: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 287776; uint32_t buffer_offset
	}, ; 19: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 302960; uint32_t buffer_offset
	}, ; 20: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 318144; uint32_t buffer_offset
	}, ; 21: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 333328; uint32_t buffer_offset
	}, ; 22: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 348472; uint32_t buffer_offset
	}, ; 23: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 363656; uint32_t buffer_offset
	}, ; 24: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 378800; uint32_t buffer_offset
	}, ; 25: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 393944; uint32_t buffer_offset
	}, ; 26: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 409088; uint32_t buffer_offset
	}, ; 27: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 424232; uint32_t buffer_offset
	}, ; 28: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15184, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 439376; uint32_t buffer_offset
	}, ; 29: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 454560; uint32_t buffer_offset
	}, ; 30: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 469704; uint32_t buffer_offset
	}, ; 31: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 484848; uint32_t buffer_offset
	}, ; 32: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 499992; uint32_t buffer_offset
	}, ; 33: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 7168, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 515136; uint32_t buffer_offset
	}, ; 34: _Microsoft.Android.Resource.Designer
	%struct.CompressedAssemblyDescriptor {
		i32 14848, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 522304; uint32_t buffer_offset
	}, ; 35: Microsoft.Extensions.Configuration
	%struct.CompressedAssemblyDescriptor {
		i32 6656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 537152; uint32_t buffer_offset
	}, ; 36: Microsoft.Extensions.Configuration.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 46592, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 543808; uint32_t buffer_offset
	}, ; 37: Microsoft.Extensions.DependencyInjection
	%struct.CompressedAssemblyDescriptor {
		i32 26112, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 590400; uint32_t buffer_offset
	}, ; 38: Microsoft.Extensions.DependencyInjection.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 8192, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 616512; uint32_t buffer_offset
	}, ; 39: Microsoft.Extensions.Diagnostics.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 7168, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 624704; uint32_t buffer_offset
	}, ; 40: Microsoft.Extensions.FileProviders.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 6144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 631872; uint32_t buffer_offset
	}, ; 41: Microsoft.Extensions.Hosting.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 17920, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 638016; uint32_t buffer_offset
	}, ; 42: Microsoft.Extensions.Logging
	%struct.CompressedAssemblyDescriptor {
		i32 19456, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 655936; uint32_t buffer_offset
	}, ; 43: Microsoft.Extensions.Logging.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 16896, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 675392; uint32_t buffer_offset
	}, ; 44: Microsoft.Extensions.Options
	%struct.CompressedAssemblyDescriptor {
		i32 9216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 692288; uint32_t buffer_offset
	}, ; 45: Microsoft.Extensions.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 1987368, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 701504; uint32_t buffer_offset
	}, ; 46: Microsoft.Maui.Controls
	%struct.CompressedAssemblyDescriptor {
		i32 135976, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 2688872; uint32_t buffer_offset
	}, ; 47: Microsoft.Maui.Controls.Xaml
	%struct.CompressedAssemblyDescriptor {
		i32 973312, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 2824848; uint32_t buffer_offset
	}, ; 48: Microsoft.Maui
	%struct.CompressedAssemblyDescriptor {
		i32 69632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 3798160; uint32_t buffer_offset
	}, ; 49: Microsoft.Maui.Essentials
	%struct.CompressedAssemblyDescriptor {
		i32 210216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 3867792; uint32_t buffer_offset
	}, ; 50: Microsoft.Maui.Graphics
	%struct.CompressedAssemblyDescriptor {
		i32 73728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4078008; uint32_t buffer_offset
	}, ; 51: Xamarin.AndroidX.Activity
	%struct.CompressedAssemblyDescriptor {
		i32 583680, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4151736; uint32_t buffer_offset
	}, ; 52: Xamarin.AndroidX.AppCompat
	%struct.CompressedAssemblyDescriptor {
		i32 17920, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4735416; uint32_t buffer_offset
	}, ; 53: Xamarin.AndroidX.AppCompat.AppCompatResources
	%struct.CompressedAssemblyDescriptor {
		i32 18944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4753336; uint32_t buffer_offset
	}, ; 54: Xamarin.AndroidX.CardView
	%struct.CompressedAssemblyDescriptor {
		i32 22528, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4772280; uint32_t buffer_offset
	}, ; 55: Xamarin.AndroidX.Collection.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 78336, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4794808; uint32_t buffer_offset
	}, ; 56: Xamarin.AndroidX.CoordinatorLayout
	%struct.CompressedAssemblyDescriptor {
		i32 596992, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4873144; uint32_t buffer_offset
	}, ; 57: Xamarin.AndroidX.Core
	%struct.CompressedAssemblyDescriptor {
		i32 26624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5470136; uint32_t buffer_offset
	}, ; 58: Xamarin.AndroidX.CursorAdapter
	%struct.CompressedAssemblyDescriptor {
		i32 9728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5496760; uint32_t buffer_offset
	}, ; 59: Xamarin.AndroidX.CustomView
	%struct.CompressedAssemblyDescriptor {
		i32 47104, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5506488; uint32_t buffer_offset
	}, ; 60: Xamarin.AndroidX.DrawerLayout
	%struct.CompressedAssemblyDescriptor {
		i32 236032, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5553592; uint32_t buffer_offset
	}, ; 61: Xamarin.AndroidX.Fragment
	%struct.CompressedAssemblyDescriptor {
		i32 23552, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5789624; uint32_t buffer_offset
	}, ; 62: Xamarin.AndroidX.Lifecycle.Common.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 18944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5813176; uint32_t buffer_offset
	}, ; 63: Xamarin.AndroidX.Lifecycle.LiveData.Core
	%struct.CompressedAssemblyDescriptor {
		i32 32768, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5832120; uint32_t buffer_offset
	}, ; 64: Xamarin.AndroidX.Lifecycle.ViewModel.Android
	%struct.CompressedAssemblyDescriptor {
		i32 13824, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5864888; uint32_t buffer_offset
	}, ; 65: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android
	%struct.CompressedAssemblyDescriptor {
		i32 39424, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5878712; uint32_t buffer_offset
	}, ; 66: Xamarin.AndroidX.Loader
	%struct.CompressedAssemblyDescriptor {
		i32 92672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5918136; uint32_t buffer_offset
	}, ; 67: Xamarin.AndroidX.Navigation.Common.Android
	%struct.CompressedAssemblyDescriptor {
		i32 19456, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6010808; uint32_t buffer_offset
	}, ; 68: Xamarin.AndroidX.Navigation.Fragment
	%struct.CompressedAssemblyDescriptor {
		i32 65536, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6030264; uint32_t buffer_offset
	}, ; 69: Xamarin.AndroidX.Navigation.Runtime.Android
	%struct.CompressedAssemblyDescriptor {
		i32 27136, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6095800; uint32_t buffer_offset
	}, ; 70: Xamarin.AndroidX.Navigation.UI
	%struct.CompressedAssemblyDescriptor {
		i32 457728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6122936; uint32_t buffer_offset
	}, ; 71: Xamarin.AndroidX.RecyclerView
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6580664; uint32_t buffer_offset
	}, ; 72: Xamarin.AndroidX.SavedState.SavedState.Android
	%struct.CompressedAssemblyDescriptor {
		i32 41984, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6592952; uint32_t buffer_offset
	}, ; 73: Xamarin.AndroidX.SwipeRefreshLayout
	%struct.CompressedAssemblyDescriptor {
		i32 62976, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6634936; uint32_t buffer_offset
	}, ; 74: Xamarin.AndroidX.ViewPager
	%struct.CompressedAssemblyDescriptor {
		i32 40448, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6697912; uint32_t buffer_offset
	}, ; 75: Xamarin.AndroidX.ViewPager2
	%struct.CompressedAssemblyDescriptor {
		i32 982016, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6738360; uint32_t buffer_offset
	}, ; 76: Xamarin.Google.Android.Material
	%struct.CompressedAssemblyDescriptor {
		i32 90624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7720376; uint32_t buffer_offset
	}, ; 77: Xamarin.Kotlin.StdLib
	%struct.CompressedAssemblyDescriptor {
		i32 28672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7811000; uint32_t buffer_offset
	}, ; 78: Xamarin.KotlinX.Coroutines.Core.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 91648, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7839672; uint32_t buffer_offset
	}, ; 79: Xamarin.KotlinX.Serialization.Core.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 201728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7931320; uint32_t buffer_offset
	}, ; 80: MetaLol.Standard.Mobile
	%struct.CompressedAssemblyDescriptor {
		i32 24064, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8133048; uint32_t buffer_offset
	}, ; 81: System.Collections.Concurrent
	%struct.CompressedAssemblyDescriptor {
		i32 15872, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8157112; uint32_t buffer_offset
	}, ; 82: System.Collections.NonGeneric
	%struct.CompressedAssemblyDescriptor {
		i32 10752, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8172984; uint32_t buffer_offset
	}, ; 83: System.Collections.Specialized
	%struct.CompressedAssemblyDescriptor {
		i32 31232, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8183736; uint32_t buffer_offset
	}, ; 84: System.Collections
	%struct.CompressedAssemblyDescriptor {
		i32 11776, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8214968; uint32_t buffer_offset
	}, ; 85: System.ComponentModel.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 94720, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8226744; uint32_t buffer_offset
	}, ; 86: System.ComponentModel.TypeConverter
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8321464; uint32_t buffer_offset
	}, ; 87: System.ComponentModel
	%struct.CompressedAssemblyDescriptor {
		i32 11776, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8326584; uint32_t buffer_offset
	}, ; 88: System.Console
	%struct.CompressedAssemblyDescriptor {
		i32 40448, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8338360; uint32_t buffer_offset
	}, ; 89: System.Diagnostics.DiagnosticSource
	%struct.CompressedAssemblyDescriptor {
		i32 11776, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8378808; uint32_t buffer_offset
	}, ; 90: System.Diagnostics.TraceSource
	%struct.CompressedAssemblyDescriptor {
		i32 11776, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8390584; uint32_t buffer_offset
	}, ; 91: System.Drawing.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8402360; uint32_t buffer_offset
	}, ; 92: System.Drawing
	%struct.CompressedAssemblyDescriptor {
		i32 60416, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8407480; uint32_t buffer_offset
	}, ; 93: System.Formats.Asn1
	%struct.CompressedAssemblyDescriptor {
		i32 22016, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8467896; uint32_t buffer_offset
	}, ; 94: System.IO.Compression.Brotli
	%struct.CompressedAssemblyDescriptor {
		i32 31744, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8489912; uint32_t buffer_offset
	}, ; 95: System.IO.Compression
	%struct.CompressedAssemblyDescriptor {
		i32 6144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8521656; uint32_t buffer_offset
	}, ; 96: System.IO.Pipelines
	%struct.CompressedAssemblyDescriptor {
		i32 354816, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8527800; uint32_t buffer_offset
	}, ; 97: System.Linq.Expressions
	%struct.CompressedAssemblyDescriptor {
		i32 69632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8882616; uint32_t buffer_offset
	}, ; 98: System.Linq
	%struct.CompressedAssemblyDescriptor {
		i32 14336, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8952248; uint32_t buffer_offset
	}, ; 99: System.Memory
	%struct.CompressedAssemblyDescriptor {
		i32 126464, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8966584; uint32_t buffer_offset
	}, ; 100: System.Net.Http
	%struct.CompressedAssemblyDescriptor {
		i32 38912, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9093048; uint32_t buffer_offset
	}, ; 101: System.Net.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 7168, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9131960; uint32_t buffer_offset
	}, ; 102: System.Net.Requests
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9139128; uint32_t buffer_offset
	}, ; 103: System.Numerics.Vectors
	%struct.CompressedAssemblyDescriptor {
		i32 17920, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9144248; uint32_t buffer_offset
	}, ; 104: System.ObjectModel
	%struct.CompressedAssemblyDescriptor {
		i32 72704, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9162168; uint32_t buffer_offset
	}, ; 105: System.Private.Uri
	%struct.CompressedAssemblyDescriptor {
		i32 396800, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9234872; uint32_t buffer_offset
	}, ; 106: System.Private.Xml
	%struct.CompressedAssemblyDescriptor {
		i32 9216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9631672; uint32_t buffer_offset
	}, ; 107: System.Runtime.InteropServices
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9640888; uint32_t buffer_offset
	}, ; 108: System.Runtime.Loader
	%struct.CompressedAssemblyDescriptor {
		i32 77312, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9646008; uint32_t buffer_offset
	}, ; 109: System.Runtime.Numerics
	%struct.CompressedAssemblyDescriptor {
		i32 14336, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9723320; uint32_t buffer_offset
	}, ; 110: System.Runtime
	%struct.CompressedAssemblyDescriptor {
		i32 123904, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9737656; uint32_t buffer_offset
	}, ; 111: System.Security.Cryptography
	%struct.CompressedAssemblyDescriptor {
		i32 29696, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9861560; uint32_t buffer_offset
	}, ; 112: System.Text.Encodings.Web
	%struct.CompressedAssemblyDescriptor {
		i32 378368, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9891256; uint32_t buffer_offset
	}, ; 113: System.Text.Json
	%struct.CompressedAssemblyDescriptor {
		i32 324608, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10269624; uint32_t buffer_offset
	}, ; 114: System.Text.RegularExpressions
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10594232; uint32_t buffer_offset
	}, ; 115: System.Threading.Thread
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10599352; uint32_t buffer_offset
	}, ; 116: System.Threading
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10611640; uint32_t buffer_offset
	}, ; 117: System.Xml.ReaderWriter
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10616760; uint32_t buffer_offset
	}, ; 118: System
	%struct.CompressedAssemblyDescriptor {
		i32 1950208, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10621880; uint32_t buffer_offset
	}, ; 119: System.Private.CoreLib
	%struct.CompressedAssemblyDescriptor {
		i32 171008, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12572088; uint32_t buffer_offset
	}, ; 120: Java.Interop
	%struct.CompressedAssemblyDescriptor {
		i32 22328, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12743096; uint32_t buffer_offset
	}, ; 121: Mono.Android.Runtime
	%struct.CompressedAssemblyDescriptor {
		i32 2135552, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12765424; uint32_t buffer_offset
	} ; 122: Mono.Android
], align 4

@uncompressed_assemblies_data_size = dso_local local_unnamed_addr constant i32 14900976, align 4

@uncompressed_assemblies_data_buffer = dso_local local_unnamed_addr global [14900976 x i8] zeroinitializer, align 1

; Metadata
!llvm.module.flags = !{!0, !1, !7}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!".NET for Android remotes/origin/release/10.0.1xx @ db080b91dbe7947a69005a203ea98d9bb207ad04"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"min_enum_size", i32 4}

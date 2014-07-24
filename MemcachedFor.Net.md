##Windows下Memcached在.Net程序中的实际运用(从Memcached库的编译到实际项目运用)
>1、**一点基础概念**

>2、**获取EnyimMemcached客户端的源代码并编译出动态库**

>3、**Memcached的服务器安装(windows server)**

>4、**基本命令介绍**

>5、**在web项目中实战**



###**一、基础概念**
>**memcached是什么**？memcached是分布式缓存系统，特点是高性能、分布式内存缓存系统。<br/>
>**memcached能做什么**？用来给动态web提升响应速度（通过缓存数据，减少数据库访问压力）。<br/>
>**为什么要用memcached**？笔者认为使用它的原因是能提升网站整体性能，减少数据库的的请求压力。**据某位博主说合理使用Memcached可以将单台机子的并发从20提升到1000.也就是说提升了50倍，笔者未经考证,有测试的同学可以留言告知楼主**


###**二、获取EnyimMemcached客户端的源代码并编译出动态库**
>你可以直接通过svn地址：→[Source](https://github.com/AlexJake/MemcachedForDotNet)←直接checkout源代码，此处获取的源代码编译之后就能得到我们在实际项目中需要使用dll(`Enyim.Caching.dll`)文件,**此源码编译出来的是4.0的dll。如果需要2.0的dll那么在获取源代码之后只需要将对应的项目更改成2.0之后，并把`log4net`目录下的对应版本的log4net.dll拷贝到`binaries`目录下即可**。然后重新编译即可获得对应版本的`Enyim.Caching.dll`的文件和`log4net.xml`文件。当然、你也可以使用兼容配置，不知道怎么配置？快去google吧！<br/>如果你的程序没有使用强签名的话。压根不用担心这个问题。我已经将源码中的签名去掉了。值得注意的是、在你的项目中、你除了要引用`Enyim.Caching.dll`之外还必须保证`log4net.xml`和它放在同个目录下。<br/>另外放一个多语言版本的下载地址，→[点我下载多语言支持版本](http://code.google.com/p/memcached/wiki/Clients)←去下载多语言版本。
>
***PS:**如果地址失效了请告知博主。谢谢*


###**三、Memcached的服务端安装(windows server)**
>在windows平台下安装和管理Memcached是很方便的。下载[MemCacheD Manager](http://memcached-manager.software.informer.com/) 下载完成之后我们甚至不需要下载Memcached的服务器端就能通过GUI界面来管理我们的多个MemCached服务，笔者在windows server2008 R2 sp1下亲测是可以使用的。**但是在同样系统环境下时在云主机上无法直接使用该软件，需要先通过下载Memcached的windows安装程序通过命令直接安装，该软件在添加服务实例的时候一直报找不到网络路径的错误，猜测可能是权限或者网卡问题。但是笔者未找到解决办法。笔者使用的是阿里云主机**。<br/>
>如果在其他版本下无法使用请留言告知博主，谢谢。最后让我们感谢伟大的**[informer](http://software.informer.com)**为我们提供的好工具。<br/><br/>
>安装完成之后、在服务中就可以看到你刚才启动的Memcached的服务、如果不出意外的话，大家可以通过`telnet ipaddress port`的命令来远程到指定主机管理和查看该memcached服务的状态，当然、前提是你的服务器和客户端都开启了telnet服务。查看命令为`stats`就可以看到服务相关的运行参数<br/><br/>

>这些配置都是可以自己指定的。如果你是使用的MemCacheDManager那么你是可以直接通过该软件的界面来直接指定Memcached的监听端口以及ip、内存大小、key大小、最大连接数等，默认端口是11211。如果使用命令行的话则需要在启动命令中加上指定的参数信息：<br/>
>>**-p <num>**设置TCP端口号(默认不设置为: 11211)<br/>
**-U <num>**UDP监听端口(默认: 11211, 0 时关闭) <br/>
**-l <ip_addr>**  绑定地址(默认:所有都允许,无论内外网或者本机更换IP，有安全隐患，若设置为127.0.0.1就只能本机访问)<br/>
**-d** 以daemon方式运行<br/>
**-u** <username> 绑定使用指定用于运行进程<username><br/>
**-m <num>**允许最大内存用量，单位M (默认: 64 MB)<br/>
**-P <file>**将PID写入文件<file>，这样可以使得后边进行快速进程终止, 需要与-d 一起使用<br/>
在linux下：./usr/local/bin/memcached -d -u root  -l 192.168.1.197 -m 2048 -p 12121<br/>
在window下：{安装路径}\memcached.exe -d RunService -l 127.0.0.1 -p 11211 -m 500<br/>
在windows下注册为服务后运行：<br/>
sc.exe create Memcached_srv binpath= “{安装路径}\memcached.exe -d RunService -p 11211 -m 500″start= auto
net start Memcached<br/><br/>
可以参考这篇博文[**Memcached 命令解析**](http://blog.csdn.net/zzulp/article/details/7823511)


###**四、基本命令介绍**
>Memcached的命令格式为：
>`<command name> <key> <flags> <exptime> <bytes> <data block>`<br/>
>>`<command name>`：命令类型[set,add,get,gets,replace]等等<br/>
>>`<key>`：关键字<br/>
>>`<flags>`：存储主键之外的额外信息<br/>
>>`<exptime>`：数据有效期、0时为永久，单位是秒(s)<br/>
>>`<bytes>`：存储的字节数<br/>
>>`<data block>`：数据的存储块(可以理解为k-v中的v)

>特别需要说明下的是如果你在命令行下使用set 那么当你的data block长度小于bytes时将不会存储，而大于的又会报块错误信息。只有在等于的时候才会执行成功得到正确的返回。



###**五、Web实战**
Memcached有很多语言的客户端实现。本次我们使用的是EnyimMemcached客户端。通过上面获取的源代码。我们编译后获取到了Enyim.Caching的dll。只需要将该dll引入到我们的项目中即可。值得注意的是、Enyim.Caching使用了log4net。因此我们在引入的时候需要将通过源码编译出的dll以及一个log4net和log4net.xml一并带上。当让。你也可以自己从新配置log4net这并不影响使用、我们需要知道的是Enyim需要log4net的支持即可。要在web中使用Enyim首先我们需要在配置信息中对Enyim进行对应的配置，下面我们来按照这个步骤试一下吧。^_^~

**1. 在config文件中增加Enyim的配置信息**<br/>
在web.config中需要对Enyim做一些配置，在ConfigSections节点之中注册enyim节点

     <configSections>
    	<sectionGroup name="enyim.com">
    	  	<section name="memcached" type="Enyim.Caching.Configuration.MemcachedClientSection, Enyim.Caching"/>
    	</sectionGroup>
	</configSections>
	<enyim.com>
	    <memcached>
	      <servers>
	        <!--在这里添加你的缓存服务器地址，可以是多个，IP地址以及对应的端口-->
	        <add address="127.0.0.1" port="11211" />
	        <!--<add address="127.0.0.1" port="11211" />-->
	      </servers>
			<!--这里进行连接池大小、连接超时设置等参数的配置-->
	      <socketPool minPoolSize="10" maxPoolSize="100" connectionTimeout="00:00:10" deadTimeout="00:02:00" />
	    </memcached>
	</enyim.com>
上面就是我们需要在配置文件中的新增的配置信息，其中每个`memcached`节点下的`servers`节点的属性是可以多个的。<br/>
**2. 引入Enyim的dll文件，如果你项目没有使用log4net的话只需要将log4net的dll以及配置文件一并放在你的运行目录下即可**<br/>
值得注意的是、项目版本。如上面所说、Log4net的版本应该和Enyim.caching的版本一致。否则会导致在使用过程中出现无发加载log4net的情况。<br/>
**3. 使用Enyim对缓存进行简单的常规数据类型进行操作。**<br/>
要进行缓存数据的操作、那么首先我们应该知道Memcached支持那些数据类型、以及在Enyim中支持了那些操作方法。<br/>
首先说一下在Enyim中支持的操作方法：
> **`append`**：向一个已经存在的key追加数据、追加的数据类型是一个`byte[]`数组，如果key不存在、则返回`false` 反之返回`true`。并且其`casUnique`的值会+1。因此可能引起cas操作的回滚。
> 
> **` bool Store`**：有五个重载。可对key执行修改、重写、和新增三个操作。并能在value的指定位置进行写入或复写数据，同时还能更新该缓存过期时间以及在间隔多少秒之后缓存失效。具体的读者可以看源代码中`MemcachedClient`类的具体的实现和注释。返回true表示成功、false表示失败。

> **` ServerStats Stats`**：获取cache服务器的运行状态。返回一个`ServerStats`类型、该类型中会包含服务器上的cache当前状态以及item数量get次数miss次数等信息。

> **` bool Remove`**：用于删除缓存中的指定key值。返回true表示成功、false表示失败

> **` void FlushAll`**：让cache中的所有item都失效、但是未清除、但是我们也没法获取。不过可以从itemscount中看到任然有那么多个item在cache中。。

> **`long Decrement`**：

> **`long Increment`**：

> **`object get`**：取回一个key对应的值。如果没找到key则返回null。

> **`T get<T>`**：取回一个T对象。如果没找到key则返回null

> **`bool CheckAndSet`**：检查并更新key对应的值。首先会检查key的casUnique值、如果这个值和本客户端最后一次获取该key的casUnique不同、则返回false。不进行更新。否则进行值的更新。这个方法是为了解决在多线程下、由于命令执行队列的非原子性可能会导致你获取该key之后、其他线程同时获取并修改了该key、而在你提交更新的时候可能会把其他线程提交的数据覆盖掉。因此或先检查获取的casUnique的值和cache中该key的是否一致。不一致则表明有其他线程已经操作过了需要你再次获取改key后重现提交修改。以保证不会弄脏缓存数据。CheckAndSet其实是`gets和cas`命令的结合使用。


**4. 使用Enyim对缓存进行非常规数据类型的操作。**<br/>

**5. 关于memcached的一些疑问补充说明**
>- **Memcached的相关操作是原子性的吗？**
>>被发送的单个命令是原子性的。你同时发送`set`和`get`命令他们不会影响对方。他们会被串行化先后执行。多线程下也是如此。但是命令的序列却不是原子性的，因此你获取一个item之后在set回去时有可能会复写被其他进程set了的item.因此在`1.2.5`以后的版本有了`gets`和`cas`的高级命令来解决这个问题。<br/>

>- **Memcached的key最大长度是多少？**<br/>
>>答案是`250`个字符。<br/>

>- **Memcached的单个item的最大大小是？**<br/>
>>答案是`1M`<br/>
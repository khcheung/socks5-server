# Edge Connection

```

/-------------\  Connected To Socks5 Server   /-------------\
|Edge Provider| ----------------------------> |Socks5 Server|
\-------------/                               \-------------/


Start New Connection

/------\ Request New Connection  /-------------\
|Client|-----------------------> |Socks5 Server|
\------/                         \-------------/

/-------------\ Request Connection From Edge    /-------------\
|Socks5 Server|-------------------------------->|Edge Provider|
\-------------/ (IP: xxx.xxx.xxx.xxx Port: ###) \-------------/

                Edge Create New Connection                   Create New Connection   
/-------------\ For This Request             /-------------\ To Dest IP & Port       /-----------\
|Socks5 Server|<-----------------------------|Edge Provider|------------------------>|Destination|
\-------------/                              \-------------/                         \-----------/

        Client Continue To   
/------\Edge Thru' Socks5    /-------------\ Use New Connection From Edge   /-------------\ Connection To Dest.   /-----------\
|Client|<------------------->|Socks5 Server|<-----------------------------> |Edge Provider|<--------------------> |Destination|
\------/                     \-------------/                                \-------------/                       \-----------/

```


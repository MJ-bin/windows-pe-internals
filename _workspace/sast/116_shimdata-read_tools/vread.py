import struct, sys
R='/home/mjbin/prj/bob/teamprj/windows-pe-internals/'
class Dump:
    def __init__(self, tag):
        self.f=open(R+f'ms/dump/03_baggage-snap/{tag}/memory.dmp','rb')
        h=self.f.read(0x2000)
        self.dtb=struct.unpack_from('<Q',h,0x10)[0]
        n,=struct.unpack_from('<I',h,0x88)
        self.runs=[]; off=0x2000
        for i in range(n):
            b,c=struct.unpack_from('<QQ',h,0x98+16*i)
            self.runs.append((b*4096,c*4096,off)); off+=c*4096
    def phys(self,pa,n):
        for b,c,off in self.runs:
            if b<=pa<b+c:
                self.f.seek(off+pa-b); return self.f.read(n)
        return None
    def q(self,pa):
        d=self.phys(pa,8)
        return struct.unpack('<Q',d)[0] if d else None
    def walk(self,va,cr3=None):
        cr3=(cr3 or self.dtb)&~0xfff
        idx=[(va>>39)&0x1ff,(va>>30)&0x1ff,(va>>21)&0x1ff,(va>>12)&0x1ff]
        t=cr3
        for lvl,i in enumerate(idx):
            e=self.q(t+8*i)
            if e is None or not e&1: return ('notpresent',lvl,e)
            if lvl==1 and e&0x80: return ((e&0x000fffffc0000000)|(va&0x3fffffff),)
            if lvl==2 and e&0x80: return ((e&0x000fffffffe00000)|(va&0x1fffff),)
            t=e&0x000ffffffffff000
        return (t|(va&0xfff),)
    def read(self,va,n,cr3=None):
        out=b''
        while n>0:
            r=self.walk(va,cr3)
            if len(r)!=1: out+=b'\xff'*0; return out, r
            pa=r[0]; ln=min(n,0x1000-(va&0xfff))
            d=self.phys(pa,ln)
            out+=d; va+=ln; n-=ln
        return out, None
if __name__=='__main__':
    tag=sys.argv[1]
    d=Dump(tag)
    print('dtb',hex(d.dtb))
    peb=0x9b2fa66000
    b,err=d.read(peb+0x2d8,8)
    print('peb+2d8',b.hex(),err)

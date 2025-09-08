using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InterOp.Server.Controllers
{
    [ApiController]
    [Route("api")]
    public class MockTaobaoController : ControllerBase
    {
        private record MockItem(
            long num_iid,
            string pic,
            string title,
            decimal price,
            string sales,
            string reviews,
            long cat_id,
            long cat_id2,
            string detail_url
        );

        private static readonly long SellerId = 14349340;
        private static readonly string SellerTitle = "固态硬盘超级店";
        private static readonly long ShopId = 66832262;
        private static readonly string ShopImg = "//img.alicdn.com/imgextra/i3/T15_U0Fs4cXXb1upjX.jpg";
        private static readonly string ShopUrl = "//store.taobao.com/shop/view_shop.htm?user_number_id=14349340";

        private static readonly List<MockItem> Items = new()
        {
           new(550406418792, "https://img.alicdn.com/imgextra/i2/14349340/TB2mtjirrBnpuFjSZFGXXX51pXa_!!14349340.jpg",
                "新品链接快递差价或者 1元货款运费邮费差价 1元货款运费邮费差价", 1m, "100以内", "-", 50023725, 50023724,
                "https://item.taobao.com/item.htm?id=550406418792"),

            new(607454902338, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01RV9ZwS2Irlr6omqcG_!!14349340.jpg",
                "Intel/英特尔S4510 240G 480G 960G 1.92T 3.84T 7.68T企业级硬盘", 583m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=607454902338"),

            new(607558919391, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01oiaCh32IrluYK5KZr_!!14349340.jpg",
                "安费诺SlimSAS 4i PLUG STR To SFF 8639 Nvme U.2 SSD硬盘数据线", 119m, "100以内", "-", 50010259, 50024099,
                "https://item.taobao.com/item.htm?id=607558919391"),

            new(608909332617, "https://img.alicdn.com/imgextra/i1/14349340/O1CN01M6Z6Pc2IrlpfLzQPh_!!14349340.jpg",
                "安费诺 Intel NVME连接线 U.2 U2数据线 固态硬盘数据线75厘米长", 89m, "100以内", "-", 50010259, 50024099,
                "https://item.taobao.com/item.htm?id=608909332617"),

            new(632372841927, "https://img.alicdn.com/imgextra/i1/14349340/O1CN01DeEABB2Irm4kThRPO_!!14349340.jpg",
                "镁光5300PRO 960G SATA 2.5企业级服务器固态硬盘SSD", 769m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=632372841927"),

            new(632906955907, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01BChShZ2IrltS5U0Cj_!!14349340.jpg",
                "全新安费诺原装Oculink连接线 U.2固态硬盘转接线", 104m, "100以内", "-", 50010259, 50024099,
                "https://item.taobao.com/item.htm?id=632906955907"),

            new(633487616390, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01Ow7EIt2IrlyP0E5Tc_!!14349340.jpg",
                "M.2全长转PCIE卡 m2转接卡 2280 22110 2260 2242 NVME固态盘转接", 25m, "100以内", "4", 124208009, 11,
                "https://item.taobao.com/item.htm?id=633487616390"),

            new(634327769292, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01SUKYUq2IrltlByOj7_!!14349340.jpg",
                "U2转接卡SSD固态硬盘转PCIE 转接卡 U.2转PCIE 免供电全新", 39m, "100以内", "28", 50003333, 11,
                "https://item.taobao.com/item.htm?id=634327769292"),

            new(641135464484, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01K8Mo8e2IrlvQpeMSR_!!14349340.jpg",
                "安费诺原厂 PCIe X4 转 U.2(SFF-8639) SSD NVMe PCIe转接线缆", 109m, "100以内", "5", 124208009, 11,
                "https://item.taobao.com/item.htm?id=641135464484"),

            new(652967879678, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01Qp5Dq72Irm57mHqlW_!!14349340.jpg",
                "双口U2转PCIE卡 安费诺连接器/双盘位全高U.2转PCIE 8X 转接卡", 265m, "100以内", "4", 124208009, 11,
                "https://item.taobao.com/item.htm?id=652967879678"),

            new(653194181048, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01M5Sssh2IrlxJXIphG_!!14349340.jpg",
                "U.2固态硬盘转PCie3.0转接板U2扩展卡PCI-E接口4口U2转PCIEX16", 489m, "100以内", "-", 124208009, 11,
                "https://item.taobao.com/item.htm?id=653194181048"),

            new(675070002496, "https://img.alicdn.com/imgextra/i1/14349340/O1CN01bsnL3A2Irm0qOtaxV_!!14349340.jpg",
                "M.2转PCIe x4转接卡 2280 22110拓展卡 NVME固态硬盘 1U2U服务", 27m, "100以内", "-", 124208009, 11,
                "https://item.taobao.com/item.htm?id=675070002496"),

            new(709967030559, "https://img.alicdn.com/imgextra/i3/14349340/O1CN016adeZp2IrmCsk56q8_!!14349340.jpg",
                "Intel/英特尔 P4800X 375G 750G 半高卡式 NVME SSD固态硬盘全新", 899m, "100以内", "12", 50013151, 11,
                "https://item.taobao.com/item.htm?id=709967030559"),

            new(710899942301, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01yeR9cD2IrmJvHjOnV_!!14349340.png",
                "Intel/英特尔 900p 280G PCI-E AIC插卡式 傲腾全新NVME 固态SSD", 539m, "100以内", "100+", 50013151, 11,
                "https://item.taobao.com/item.htm?id=710899942301"),

            new(711844297282, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01Le09jh2Irm58eNTL0_!!14349340.png",
                "固态硬盘超级店#买家秀征集#", 0.99m, "100以内", "-", 50023728, 50023724,
                "https://item.taobao.com/item.htm?id=711844297282"),

            new(711911516407, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01Jf4aKf2Irm5EA7pon_!!14349340.jpg",
                "Intel安费诺U2 U.2转M2 M.2 MiniSAS SlimSAS PCIe固态硬盘转接线", 65m, "100以内", "22", 50010259, 50024099,
                "https://item.taobao.com/item.htm?id=711911516407"),

            new(719013680041, "https://img.alicdn.com/imgextra/i4/14349340/O1CN016zkjuU2Irm6BJuHDc_!!14349340.jpg",
                "Intel/英特尔 P4610 3.2T 6.4T U.2 NVME PCIE固态硬盘企业级SSD", 2150m, "100以内", "1", 50013151, 11,
                "https://item.taobao.com/item.htm?id=719013680041"),

            new(721411577134, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01FddE4h2Irm67V0at2_!!14349340.jpg",
                "Intel/英特尔 P5530系列 960G U.2接口企业级固态硬盘全新SSD", 759m, "100以内", "2", 50013151, 11,
                "https://item.taobao.com/item.htm?id=721411577134"),

            new(723847385966, "https://img.alicdn.com/imgextra/i1/14349340/O1CN01NxZMCP2Irm6O05ZZb_!!14349340.jpg",
                "Intel/英特尔 P5530 1.92T 3.84T U.2 4.0全新企业级固态硬盘国行", 1609m, "100以内", "2", 50013151, 11,
                "https://item.taobao.com/item.htm?id=723847385966"),

            new(733193215881, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01p5EtBU2Irm17BmbB1_!!14349340.jpg",
                "Intel/英特尔 P4610 1.6T U.2 接口 NVME 固态硬盘SSD 全新盒装", 970m, "100以内", "2", 50013151, 11,
                "https://item.taobao.com/item.htm?id=733193215881"),

            new(739749105932, "https://img.alicdn.com/imgextra/i2/14349340/O1CN016cJNE62Irm8OX5ZX7_!!14349340.jpg",
                "Intel/英特尔 傲腾P5801X 400G E1.S接口 企业级固态硬盘全新SSD", 1750m, "100以内", "51", 50013151, 11,
                "https://item.taobao.com/item.htm?id=739749105932"),

            new(741181438030, "https://img.alicdn.com/imgextra/i1/14349340/O1CN01k1xsbR2Irm8TSY8Qq_!!14349340.jpg",
                "Intel/英特尔 P5600系列 3.2T U.2 4.0接口企业级固态硬盘全新SSD", 2729m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=741181438030"),

            new(743796457925, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01iLQrBh2IrmCO2u1lS_!!14349340.jpg",
                "英摩客 免供电U.3转全高卡式PCI-e X4 转接卡 小绿卡 UMC-PTU3-S", 64m, "100以内", "5", 124208009, 11,
                "https://item.taobao.com/item.htm?id=743796457925"),

            new(755421342979, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01KmyBtw2IrmA7Cgevj_!!14349340.jpg",
                "Micron/美光 5200PRO 960G 2.5寸 SATA 企业级服务器固态硬盘SSD", 715m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=755421342979"),

            new(760480907554, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01TpAW9k2IrmASRD0KB_!!14349340.jpg",
                "Intel/英特尔 P5600 1.6T U.2 4.0接口 TLC企业级固态硬盘全新SSD", 1299m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=760480907554"),

            new(773951223385, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01CHaXec2IrmBmitkHz_!!14349340.jpg",
                "全新镁光5300 PRO 480G M.2 2280企业级固态硬盘SATA协议SSD", 429m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=773951223385"),

            new(773969226015, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01pKb2W12IrmBp6Ok9e_!!14349340.jpg",
                "Kioxia/铠侠 CM6-R 7.68T U.2 4.0接口NVME服务器企业级固态硬盘", 4929m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=773969226015"),

            new(776181959871, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01CHaXec2IrmBmitkHz_!!14349340.jpg",
                "Micron/美光 5300PRO 480G M.2 2280 NGFF 企业级固态硬盘SSD全新", 429m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=776181959871"),

            new(779257606287, "https://img.alicdn.com/imgextra/i4/14349340/O1CN0147Opw52IrmCKalF27_!!14349340.jpg",
                "Kioxia/铠侠 CD8 7.68T U.2 4.0接口NVME服务器企业级固态硬盘", 5099m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=779257606287"),

            new(779270344615, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01DUGkee2IrmCERF8cc_!!14349340.jpg",
                "Micron/镁光 7450 PRO 960G M2 22110美光服务器SSD企业级固态硬盘", 669m, "100以内", "0", 50013151, 11,
                "https://item.taobao.com/item.htm?id=779270344615"),

            new(783548202405, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01GaQDlJ2IrmCZCmwtX_!!14349340.jpg",
                "英摩客 黑E卡 转接卡 E1.s/E3.s 接口 转换为半高卡式的PCIe 全新", 209m, "100以内", "2", 124208009, 11,
                "https://item.taobao.com/item.htm?id=783548202405"),

            new(789846576061, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01AOVnL52IrmCvEMsvk_!!14349340.jpg",
                "Micron/美光 5200ECO 960G 2.5寸 SATA 企业级服务器固态硬盘SSD", 605m, "100以内", "1", 50013151, 11,
                "https://item.taobao.com/item.htm?id=789846576061"),

            new(790195185494, "https://img.alicdn.com/imgextra/i1/14349340/O1CN010VtiOz2IrmCx33dCE_!!14349340.jpg",
                "Micron/美光 7400MAX 1.6T U.3 4.0 企业级服务器SSD固态硬盘全新", 1299m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=790195185494"),

            new(829337512104, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01oqYzYu2IrmEuXHPt9_!!14349340.jpg",
                "DERA/得瑞 D5427系列 3.84T 半高卡式 企业级固态硬盘SSD 全新", 1399m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=829337512104"),

            new(832997062710, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01MgHjzZ2IrmF6TXD1P_!!14349340.jpg",
                "DERA/得瑞 D5437系列 3.84T U.2 3.0 NVME企业级固态硬盘SSD 全新", 1499m, "100以内", "6", 201309422, 11,
                "https://item.taobao.com/item.htm?id=832997062710"),

            new(833416392279, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01npkzO32IrmF9Typve_!!14349340.jpg",
                "DERA/得瑞 D5437系列 1.92T 2T U.2 NVME 企业级固态硬盘SSD全新", 739m, "100以内", "12", 201309422, 11,
                "https://item.taobao.com/item.htm?id=833416392279"),

            new(890273318885, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01P1ayS72IrmHxwfhys_!!14349340.jpg",
                "Intel/英特尔 P5500 3.84T U.2 4.0 固态硬盘SSDSSDPF2KX038T9N1", 3099m, "100以内", "1", 50013151, 11,
                "https://item.taobao.com/item.htm?id=890273318885"),

            new(890278998659, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01nd5L112IrmHy2lk5q_!!14349340.jpg",
                "铠侠CD8P-R 15.36T U.2 5.0接口企业级固态硬盘全新SSD", 11550m, "100以内", "1", 50013151, 11,
                "https://item.taobao.com/item.htm?id=890278998659"),

            new(890692905057, "https://img.alicdn.com/imgextra/i1/14349340/O1CN01vvKNUT2IrmHzFL1fd_!!14349340.jpg",
                "美光7450PRO 960G U.3 PCIE4.0固态硬盘MTFDKCC960TFR-1BC15ABYY", 629m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=890692905057"),

            new(891056132530, "https://img.alicdn.com/imgextra/i1/14349340/O1CN01nMD0m62IrmHxwubpZ_!!14349340.jpg",
                "Micron/美光 5300PRO 960G SATA 2.5 企业级固态硬盘SSD DELL版", 679m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=891056132530"),

            new(891059208178, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01nVS0hW2IrmI0AAriQ_!!14349340.jpg",
                "Kioxia/铠侠 CD7-R 7.68T U.2 4.0 接口 企业级固态硬盘全新 SSD", 4729m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=891059208178"),

            new(912572226688, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01pKw0At2IrmIsAGKC2_!!14349340.jpg",
                "Micron/美光 7400PRO 3.84T E1.s接口 企业级服务器固态硬盘 全新", 1610m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=912572226688"),

            new(915699278391, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01KBv58a2IrmJ2W2q1y_!!14349340.jpg",
                "Intel/英特尔 P5510 7.68T U.2 4.0 企业级固态硬盘全新SSD 国行", 5350m, "100以内", "1", 50013151, 11,
                "https://item.taobao.com/item.htm?id=915699278391"),

            new(952207663736, "https://img.alicdn.com/imgextra/i1/14349340/O1CN015M0Uxr2IrmKPKjFvb_!!14349340.jpg",
                "Solidigm PS1010系列 3.84T U.2 5.0 NVME 企业级固态硬盘SSD全新", 4005m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=952207663736"),

            new(952209635353, "https://img.alicdn.com/imgextra/i2/14349340/O1CN013gSLnt2IrmKQ6aTRo_!!14349340.jpg",
                "Solidigm PS1010系列 1.92T U.2 5.0 NVME 企业级固态硬盘SSD全新", 2820m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=952209635353"),

            new(952665066061, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01hF9wzJ2IrmKQaxGs4_!!14349340.jpg",
                "Solidigm PS1030系列 3.2T U.2 5.0 NVME 企业级固态硬盘SSD 全新", 4410m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=952665066061"),

            new(953306869793, "https://img.alicdn.com/imgextra/i1/14349340/O1CN01UarrJ62IrmKKSUZB4_!!14349340.jpg",
                "Solidigm PS1030系列 1.6T U.2 5.0 NVME 企业级固态硬盘SSD 全新", 2499m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=953306869793"),

            new(953312205299, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01AoYekq2IrmKQ4xaBO_!!14349340.jpg",
                "Solidigm PS1010系列 15.36T U.2 5.0 NVME企业级固态硬盘SSD全新", 13888m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=953312205299"),

            new(953881812654, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01UtN8z32IrmKKSsbGl_!!14349340.jpg",
                "Solidigm PS1010系列 7.68T U.2 5.0 NVME 企业级固态硬盘SSD全新", 7525m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=953881812654"),

            new(961492343673, "https://img.alicdn.com/imgextra/i4/14349340/O1CN017M0A3C2IrmKqhmO6O_!!14349340.jpg",
                "Memblaze/忆恒创源 PBlaze6 6547 3.2T U.2 4.0 固态硬盘SSD 全新", 1999m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=961492343673"),

            new(961494179396, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01XixSV52IrmKpZMThP_!!14349340.jpg",
                "Union Memory/忆联 UH831a 3.2T U.2 4.0 企业级固态硬盘SSD 全新", 1899m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=961494179396"),

            new(961963078701, "https://img.alicdn.com/imgextra/i4/14349340/O1CN01xAI9vb2IrmKq3owt6_!!14349340.jpg",
                "Memblaze/忆恒创源 PBlaze7 7940 7.68T U.2 5.0 固态硬盘SSD全新", 4409m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=961963078701"),

            new(962626945661, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01sP0yaD2IrmKozRQSn_!!14349340.jpg",
                "Memblaze/忆恒创源 PBlaze7 7940 3.84T U.2 5.0 固态硬盘SSD全新", 2499m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=962626945661"),

            new(962636429705, "https://img.alicdn.com/imgextra/i1/14349340/O1CN015xH5MX2IrmKqRGign_!!14349340.jpg",
                "Memblaze/忆恒创源 PBlaze6 6541 3.84T U.2 4.0 固态硬盘SSD全新", 2299m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=962636429705"),

            new(962642665159, "https://img.alicdn.com/imgextra/i2/14349340/O1CN01s06EjR2IrmKqfsEOq_!!14349340.jpg",
                "Union Memory/忆联 UH831a 6.4T U.2 4.0 企业级固态硬盘SSD 全新", 3505m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=962642665159"),

            new(963234636982, "https://img.alicdn.com/imgextra/i3/14349340/O1CN01LxZHyo2IrmKq4lgwG_!!14349340.jpg",
                "Memblaze/忆恒创源 PBlaze7 7946 3.2T U.2 5.0 固态硬盘SSD 全新", 2499m, "100以内", "-", 50013151, 11,
                "https://item.taobao.com/item.htm?id=963234636982")
        };

        [HttpGet]
        public IActionResult Get([FromQuery] string? api,
                                 [FromQuery(Name = "seller_id")] long? sellerId,
                                 [FromQuery] int page = 1,
                                 [FromQuery(Name = "page_size")] int pageSize = 60)
        {
            if (!string.Equals(api, "shop_items", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = "Unsupported mock api. Use api=shop_items" });

            if (sellerId is null || sellerId != SellerId)
                return BadRequest(new { message = "Unknown seller_id in mock.", expected = SellerId, got = sellerId });

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 60;

            var total = Items.Count;
            var totalPage = (int)Math.Ceiling(total / (double)pageSize);
            if (totalPage == 0) totalPage = 1;
            if (page > totalPage) page = totalPage;

            var slice = Items.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(i => new
                {
                    i.num_iid,
                    i.pic,
                    i.title,
                    i.price,
                    i.sales,
                    i.reviews,
                    i.detail_url,
                    i.cat_id,
                    i.cat_id2
                })
                .ToArray();

            var payload = new
            {
                result = new
                {
                    status = new { msg = "success", code = 200, execution_time = "0.001" },
                    seller_id = SellerId,
                    seller_title = SellerTitle,
                    shop_id = ShopId,
                    shop_img = ShopImg,
                    shop_url = ShopUrl,
                    page,
                    page_size = pageSize,
                    total_results = total,
                    total_page = totalPage,
                    item = slice
                }
            };

            return Ok(payload);
        }
    }
}

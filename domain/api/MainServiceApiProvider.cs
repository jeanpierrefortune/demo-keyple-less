using DemoKeypleLess.domain.spi;

namespace DemoKeypleLess.domain.api {
    internal class MainServiceApiProvider {
        public static MainServiceApi getService(ReaderServiceSpi readerService, ServerSpi server)
        {
            return new MainServiceAdapter ( readerService, server );
        }
    }
}

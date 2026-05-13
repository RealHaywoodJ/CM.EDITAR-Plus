# Created: %DATE%
# Author:  %USERNAME%

FROM alpine:3.20

WORKDIR /app
COPY . .

CMD ["sh"]

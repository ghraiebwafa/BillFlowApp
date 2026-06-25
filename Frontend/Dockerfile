FROM nginx:alpine

COPY index.html /usr/share/nginx/html/index.html

EXPOSE 3000

CMD ["sh", "-c", "sed -i 's/listen       80;/listen 3000;/g' /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"]

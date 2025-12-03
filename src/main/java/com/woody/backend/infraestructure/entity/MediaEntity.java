package com.woody.backend.infraestructure.entity;

import java.time.LocalDate;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.ManyToOne;

import com.woody.backend.infraestructure.entity.enums.MediaType;

@Entity
public class MediaEntity {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private long id;

    @ManyToOne
    private PostEntity post;

    private String url;

    private MediaType mediaType;
    
    private LocalDate created_at;

    public MediaEntity(){}

    public MediaEntity(PostEntity post, String url, MediaType mediaType, LocalDate created_at) {
        this.post = post;
        this.url = url;
        this.mediaType = mediaType;
        this.created_at = created_at;
    }

    public PostEntity getPost() {
        return post;
    }

    public void setPost(PostEntity post) {
        this.post = post;
    }

    public String getUrl() {
        return url;
    }

    public void setUrl(String url) {
        this.url = url;
    }

    public MediaType getMediaType() {
        return mediaType;
    }

    public void setMediaType(MediaType mediaType) {
        this.mediaType = mediaType;
    }

    public LocalDate getCreated_at() {
        return created_at;
    }

    public void setCreated_at(LocalDate created_at) {
        this.created_at = created_at;
    }

    public long getId() {
        return id;
    }
}

package com.woody.backend.infraestructure.entity;

import java.time.LocalDate;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.ManyToOne;

@Entity
public class PostEntity {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private long id;

    @ManyToOne
    private UserEntity user;

    @ManyToOne
    private TopicEntity topic;

    @ManyToOne
    private PostEntity parentPost;

    private String description;

    private boolean is_active;
    
    private LocalDate created_at;
    
    private LocalDate updated_at;

    public PostEntity(){}

    public PostEntity(UserEntity user, TopicEntity topic, PostEntity parentPost,
            String description, boolean is_active, LocalDate created_at, LocalDate updated_at) {
        this.user = user;
        this.topic = topic;
        this.parentPost = parentPost;
        this.description = description;
        this.is_active = is_active;
        this.created_at = created_at;
        this.updated_at = updated_at;
    }

    public UserEntity getIdUser() {
        return user;
    }

    public void setIdUser(UserEntity user) {
        this.user = user;
    }

    public TopicEntity getIdTopic() {
        return topic;
    }

    public void setIdTopic(TopicEntity topic) {
        this.topic = topic;
    }

    public PostEntity getParentPostId() {
        return parentPost;
    }

    public void setParentPostId(PostEntity parentPost) {
        this.parentPost = parentPost;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public boolean isIs_active() {
        return is_active;
    }

    public void setIs_active(boolean is_active) {
        this.is_active = is_active;
    }

    public LocalDate getCreated_at() {
        return created_at;
    }

    public void setCreated_at(LocalDate created_at) {
        this.created_at = created_at;
    }

    public LocalDate getUpdated_at() {
        return updated_at;
    }

    public void setUpdated_at(LocalDate updated_at) {
        this.updated_at = updated_at;
    }

    public long getId() {
        return id;
    }
}
